﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;

namespace NLite.Linq
{
#if SDK4
    class ForEachExpression : CustomExpression
    {

        readonly ParameterExpression variable;
        readonly Expression enumerable;
        readonly Expression body;

        readonly LabelTarget break_target;
        readonly LabelTarget continue_target;

        public new ParameterExpression Variable
        {
            get { return variable; }
        }

        public Expression Enumerable
        {
            get { return enumerable; }
        }

        public Expression Body
        {
            get { return body; }
        }

        public LabelTarget BreakTarget
        {
            get { return break_target; }
        }

        public LabelTarget ContinueTarget
        {
            get { return continue_target; }
        }

        public override Type Type
        {
            get
            {
                if (break_target != null)
                    return break_target.Type;

                return typeof(void);
            }
        }

        public override CustomExpressionType CustomNodeType
        {
            get { return CustomExpressionType.ForEachExpression; }
        }

        internal ForEachExpression(ParameterExpression variable, Expression enumerable, Expression body, LabelTarget break_target, LabelTarget continue_target)
        {
            this.variable = variable;
            this.enumerable = enumerable;
            this.body = body;
            this.break_target = break_target;
            this.continue_target = continue_target;
        }

        public ForEachExpression Update(ParameterExpression variable, Expression enumerable, Expression body, LabelTarget breakTarget, LabelTarget continueTarget)
        {
            if (this.variable == variable && this.enumerable == enumerable && this.body == body && break_target == breakTarget && continue_target == continueTarget)
                return this;

            return CustomExpression.ForEach(variable, enumerable, body, continueTarget, breakTarget);
        }

        public override Expression Reduce()
        {
            Type argument;
            Type enumerable_type;
            Type enumerator_type;
            if (!TryGetGenericEnumerableArgument(out argument))
            {
                enumerator_type = typeof(IEnumerator);
                enumerable_type = typeof(IEnumerable);
            }
            else
            {
                enumerator_type = typeof(IEnumerator<>).MakeGenericType(argument);
                enumerable_type = typeof(IEnumerable<>).MakeGenericType(argument);
            }

            var enumerator = Expression.Variable(enumerator_type);
            var disposable = Expression.Variable(typeof(IDisposable));

            var inner_loop_continue = Expression.Label("inner_loop_continue");
            var inner_loop_break = Expression.Label("inner_loop_break");
            var @continue = continue_target ?? Expression.Label("continue");
            var @break = break_target ?? Expression.Label("break");
            var end_finally = Expression.Label("end");

            return Expression.Block(
                new[] { enumerator },
                enumerator.Assign(Expression.Call(enumerable.Convert(enumerable_type), enumerable_type.GetMethod("GetEnumerator"))),
                Expression.TryFinally(
                    Expression.Block(
                        new[] { variable },
                        Expression.Goto(@continue),
                        Expression.Loop(
                            Expression.Block(
                                variable.Assign(enumerator.Property("Current").Convert(variable.Type)),
                                    body,
                                    Expression.Label(@continue),
                                    Expression.Condition(
                                        Expression.Call(
                                            enumerator,
                                            typeof(IEnumerator).GetMethod("MoveNext")),
                                        Expression.Goto(inner_loop_continue),
                                        Expression.Goto(inner_loop_break))),
                            inner_loop_break,
                            inner_loop_continue),
                        Expression.Label(@break)),
                    Expression.Block(
                        new[] { disposable },
                        disposable.Assign(enumerator.TypeAs(typeof(IDisposable))),
                        disposable.NotEqual(Expression.Constant(null)).Condition(
                            Expression.Call(disposable, typeof(IDisposable).GetMethod("Dispose", Type.EmptyTypes)),
                            Expression.Goto(end_finally)),
                        Expression.Label(end_finally))));
        }

        bool TryGetGenericEnumerableArgument(out Type argument)
        {
            argument = null;

            foreach (var iface in enumerable.Type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                var definition = iface.GetGenericTypeDefinition();
                if (definition != typeof(IEnumerable<>))
                    continue;

                argument = iface.GetGenericArguments()[0];
                if (variable.Type.IsAssignableFrom(argument))
                    return true;
            }

            return false;
        }

        protected override Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor)
        {
            return Update(
                (ParameterExpression)visitor.Visit(variable),
                visitor.Visit(enumerable),
                visitor.Visit(body),
                break_target,
                continue_target);
        }

        public override Expression Accept(CustomExpressionVisitor visitor)
        {
            return visitor.VisitForEachExpression(this);
        }
    }

    abstract partial class CustomExpression
    {

        public static ForEachExpression ForEach(ParameterExpression variable, Expression enumerable, Expression body)
        {
            return ForEach(variable, enumerable, body, null);
        }

        public static ForEachExpression ForEach(ParameterExpression variable, Expression enumerable, Expression body, LabelTarget breakTarget)
        {
            return ForEach(variable, enumerable, body, breakTarget, null);
        }

        public static ForEachExpression ForEach(ParameterExpression variable, Expression enumerable, Expression body, LabelTarget breakTarget, LabelTarget continueTarget)
        {
            if (variable == null)
                throw new ArgumentNullException("variable");
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");
            if (body == null)
                throw new ArgumentNullException("body");

            if (!typeof(IEnumerable).IsAssignableFrom(enumerable.Type))
                throw new ArgumentException("The enumerable must implement at least IEnumerable", "enumerable");

            if (continueTarget != null && continueTarget.Type != typeof(void))
                throw new ArgumentException("Continue label target must be void ", "continueTarget");

            return new ForEachExpression(variable, enumerable, body, breakTarget, continueTarget);
        }
    }
#endif
}
