// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Dynamic;
    using System.Management.Automation;

    /// <summary>
    /// Represents a dynamic object which uses a <see cref="RuntimeDefinedParameterDictionary"/> as it's source.
    /// </summary>
    public class DynamicDictionary : DynamicObject
    {
        private RuntimeDefinedParameterDictionary dictionary;

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicDictionary"/> class.
        /// </summary>
        /// <param name="dictionary"></param>
        public DynamicDictionary(RuntimeDefinedParameterDictionary dictionary)
        {
            this.dictionary = dictionary;
        }

        /// <inheritdoc />
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool isSuccessful = this.dictionary.TryGetValue(binder.Name, out RuntimeDefinedParameter parameter);
            if (isSuccessful)
            {
                object convertedValue;
                try
                {
                    convertedValue = Convert.ChangeType(parameter.Value, parameter.ParameterType);
                }
                catch
                {
                    convertedValue = Activator.CreateInstance(parameter.ParameterType);
                }

                result = convertedValue;
            }
            else
            {
                result = null;
            }

            return isSuccessful;
        }

        /// <inheritdoc />
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (this.dictionary.ContainsKey(binder.Name))
            {
                this.dictionary[binder.Name].Value = value;
                return true;
            }

            return false;
        }
    }
}
