// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;

    /// <summary>
    /// Represents an uploaded achievement image.
    /// </summary>
    public class AchievementImage
    {
        /// <summary>
        /// Gets or sets the asset ID of the achievement image.
        /// </summary>
        public Guid AssetId { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image on the CDN.
        /// </summary>
        public Uri CdnUrl { get; set; }

        /// <summary>
        /// Gets or sets the height of the image in pixels.
        /// </summary>
        public int HeightInPixels { get; set; }

        /// <summary>
        /// Gets or sets the type of image that was uploaded.
        /// </summary>
        public string ImageType { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating if the image is public or not.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the service configuration ID of the product the image was uploaded for.
        /// </summary>
        public Guid Scid { get; set; }

        /// <summary>
        /// Gets or sets the URO of the thumbnail version of the image on the CDN.
        /// </summary>
        public Uri ThumbnailCdnUrl { get; set; }

        /// <summary>
        /// Gets or sets the width of the image in pixels.
        /// </summary>
        public int WidthInPixels { get; set; }
    }
}
