// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents an uploaded achievement image.
    /// </summary>
    public class AchievementImage
    {
        /// <summary>
        /// Gets or sets the asset ID of the achievement image.
        /// </summary>
        [Display(Name = "Asset ID", Order = 1)]
        public Guid AssetId { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image on the CDN.
        /// </summary>
        [Display(Name = "URL", Order = 3)]
        public Uri CdnUrl { get; set; }

        /// <summary>
        /// Gets or sets the height of the image in pixels.
        /// </summary>
        [Display(Name = "Height", Order = 6)]
        public int HeightInPixels { get; set; }

        /// <summary>
        /// Gets or sets the type of image that was uploaded.
        /// </summary>
        [Display(Name = "Image type", Order = 7)]
        public string ImageType { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating if the image is public or not.
        /// </summary>
        [Display(Name = "Is public", Order = 8)]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the service configuration ID of the product the image was uploaded for.
        /// </summary>
        [Display(Name = "SCID", Order = 2)]
        public Guid Scid { get; set; }

        /// <summary>
        /// Gets or sets the URO of the thumbnail version of the image on the CDN.
        /// </summary>
        [Display(Name = "Thumbnail URL", Order = 4)]
        public Uri ThumbnailCdnUrl { get; set; }

        /// <summary>
        /// Gets or sets the width of the image in pixels.
        /// </summary>
        [Display(Name = "Width", Order = 5)]
        public int WidthInPixels { get; set; }
    }
}
