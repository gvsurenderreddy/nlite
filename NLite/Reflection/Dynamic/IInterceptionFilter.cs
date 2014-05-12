//
// NProxy is a library for the .NET framework to create lightweight dynamic proxies.
// Copyright © Martin Tamme
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//

using System.Reflection;

namespace NLite.Reflection.Dynamic
{
    /// <summary>
    /// Defines an interception filter.
    /// </summary>
    interface IInterceptionFilter
    {
        /// <summary>
        /// Accepts the specified event.
        /// </summary>
        /// <param name="eventInfo">The event information.</param>
        /// <returns>A value indicating whether the specified event is accepted.</returns>
        bool AcceptEvent(EventInfo eventInfo);

        /// <summary>
        /// Accepts the specified property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>A value indicating whether the specified property is accepted.</returns>
        bool AcceptProperty(PropertyInfo propertyInfo);

        /// <summary>
        /// Accepts the specified method.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>A value indicating whether the specified method is accepted.</returns>
        bool AcceptMethod(MethodInfo methodInfo);
    }
}