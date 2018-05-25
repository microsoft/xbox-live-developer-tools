// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;

    /// <summary>
    /// Provides rendering tips for members of objects, both as a single object and as a list.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DisplayAttribute : Attribute
    {
        private int? order;
        private int? listOrder;
        private bool? omit;
        private bool? listOmit;

        /// <summary>
        /// Gets or sets the display name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the order in which the property should be displayed as a single object.
        /// </summary>
        public int Order
        {
            get
            {
                if (!this.order.HasValue)
                {
                    throw new InvalidOperationException("Order has not been set. Use GetOrder() instead.");
                }

                return this.order.Value;
            }
            
            set
            {
                this.order = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether a property's value should be omitted from the displayed single object.
        /// </summary>
        public bool Omit
        {
            get
            {
                if (this.omit.HasValue)
                {
                    return this.omit.Value;
                }

                return false;
            }

            set
            {
                this.omit = value;
            }
        }

        /// <summary>
        /// Gets or sets the order in which the property should be displayed in a list.
        /// </summary>
        public int ListOrder
        {
            get
            {
                if (!this.listOrder.HasValue)
                {
                    throw new InvalidOperationException("ListOrder has not been set. Use GetListOrder() instead.");
                }

                return this.listOrder.Value;
            }

            set
            {
                this.listOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether a property's value should be omitted from a list of objects.
        /// </summary>
        public bool ListOmit
        {
            get
            {
                if (this.listOmit.HasValue)
                {
                    return this.listOmit.Value;
                }

                return false;
            }

            set
            {
                this.listOmit = value;
            }
        }

        /// <summary>
        /// Gets the order in which the property should be displayed as a single object.
        /// </summary>
        public int? GetOrder()
        {
            return this.order;
        }

        /// <summary>
        /// Gets the order in which the property should be displayed in a list.
        /// </summary>
        public int? GetListOrder()
        {
            return this.listOrder;
        }
    }
}
