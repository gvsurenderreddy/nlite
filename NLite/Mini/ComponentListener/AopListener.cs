﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLite.Interceptor;
using NLite.Interceptor.Matcher;
using NLite.Interceptor.Metadata;
using NLite.Mini.Proxy;
using NLite.Reflection;
using NLite.Interceptor.Internal;

namespace NLite.Mini.Listener
{
    /// <summary>
    /// Aop 监听器
    /// </summary>
    public class AopListener : ComponentListenerAdapter
    {
        private IAspectRepository AspectRepository;
        private IAspectMatcher AspectMatcher;
        private IInterceptorRepository InterceptorRepository;

        /// <summary>
        /// 
        /// </summary>
        protected override void Init()
        {
            AspectMatcher = new AspectMatcher();
            InterceptorRepository = new InterceptorRepository();
            AspectRepository = new AspectRepository();

            Kernel.RegisterInstance(AspectMatcher);
            Kernel.RegisterInstance(InterceptorRepository);
            Kernel.RegisterInstance(AspectRepository);
            Kernel.RegisterInstance(ProxyFactory.Default);
        }

        /// <summary>
        /// 在组件元数据注册后进行监听，如果符合代理条件的就在元数据的扩展属性里面添加一个"proxy"标记位
        /// </summary>
        /// <param name="info"></param>
        public override void OnMetadataRegistered(IComponentInfo info)
        {
            CheckAndRegisterAspect(info);

            if (HasMatch(info))
                info.ExtendedProperties["proxy"] = true;

            base.OnMetadataRegistered(info);
        }

        private void CheckAndRegisterAspect(IComponentInfo info)
        {
            AspectInfo aspect = null;

            var adviceTypes = info.Implementation.GetAttributes<InterceptorAttribute>(true).Select(p => p.InterceptorType).ToArray();
            if (adviceTypes != null && adviceTypes.Length != 0)
            {
                aspect = new AspectInfo { TargetType = new SignleTargetTypeInfo { SingleType = info.Implementation } };

                var pointCut = new PointCutInfo();

                aspect.AddPointCut(pointCut);

                pointCut.Signature = new MethodSignature
                {
                    Deep = 3,
                };

                pointCut.Advices = adviceTypes;
            }

            var pointCuts = (from m in info.Implementation.GetMethods().Where(m => m.IsPublic || m.IsFamily)
                             let attrs = m.GetAttributes<InterceptorAttribute>(true)
                             where attrs != null && attrs.Length > 0
                             select new PointCutInfo
                             {
                                 Advices = attrs.Select(p => p.InterceptorType).ToArray(),
                                 Signature = new MethodSignature
                                 {
                                     Method = m.Name,
                                     ReturnType = m.ReturnType.FullName,
                                     Flags = CutPointFlags.Method,
                                     Access = AccessFlags.All,
                                     Arguments = m.GetParameterTypes().Select(p => p.FullName).ToArray()
                                 }
                             }).ToArray();


            if (pointCuts != null && pointCuts.Length > 0)
            {
                if (aspect == null)
                    aspect = new AspectInfo();

                foreach (var pointCut in pointCuts)
                {
                    aspect.AddPointCut(pointCut);
                }
            }

            if (aspect != null)
            {
                AspectRepository.Register(aspect);
            }
        }


        private bool HasMatch(IComponentInfo info)
        {
            if (AspectRepository == null
                || AspectRepository.Aspects.Count() == 0
                || AspectMatcher == null)
                return false;

            //得到所有切面
            var aspects = AspectMatcher.Match(info.Implementation, AspectRepository.Aspects).ToArray();
            if (aspects.Length == 0)
                return false;

            //得到所有切点
            var pointCuts = (from aspect in aspects
                             from pointCut in aspect.PointCuts
                             select pointCut)
                             .Distinct()
                             .ToArray();

            if (pointCuts.Length == 0)
                return false;

            //得到所有Advice
            var advices = (from item in
                               (
                                   from pointCut in pointCuts
                                   from type in pointCut.Advices
                                   select type).Distinct()
                           select
                           new 
                           {
                              Type =  item,
                              Factory = new Func<IInterceptor>(()=>Activator.CreateInstance(item) as IInterceptor) ,
                            })
                            .ToDictionary(p=>p.Type,p=>p.Factory);

            if (advices.Count == 0)
                return false;

            var matcher = new JoinPointMatcher(pointCuts);

            //得到所有的接入点
            var joinPoints = (from item in info.Implementation  .GetMethods().Union(info.Contracts.SelectMany(p=>p.GetMethods()).Distinct())
                              from pointCut in pointCuts
                              let result = matcher.Match(info.Implementation, item).ToArray()
                              where result.Length > 0
                              select new { Method = item, PointCuts = result })
                         .ToArray();

            if (joinPoints.Length == 0)
                return false;


            foreach (var item in joinPoints)
                RegisterInteceptors(item.Method, item.PointCuts, advices);

            info.ExtendedProperties["interceptors"] = advices.Values.Distinct().ToArray();
            info.ExtendedProperties["methods"] = joinPoints.Select(p => p.Method).Distinct().ToArray();

            return true;
        }

        private void RegisterInteceptors(MethodInfo method
            , IEnumerable<ICutPointInfo> pointCuts
            , Dictionary<Type, Func<NLite.Interceptor.IInterceptor>> interceptorMap)
        {
            var temps = InterceptorRepository.Get(method);

            if (temps.Count > 0)
                return;

            foreach (var pointCut in pointCuts)
                foreach (var advice in pointCut.Advices)
                {
                    var interceptor = interceptorMap[advice];
                    if (interceptor != null)
                        temps.Add(interceptor);
                }
        }
    }
}
