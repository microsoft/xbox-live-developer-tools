// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using CERTENROLLLib;
    using Microsoft.Xbox.Services.DevTools.Common;

    /// <summary>
    /// Assists in the creation of certificate signing requests and exporting certificates.
    /// </summary>
    public class CertificateHelper : IDisposable
    {
        private SecureString privateKey;

        /// <summary>
        /// Gets a boolean value indicating that the current user can generate a cert request (i.e. running as an administrator).
        /// </summary>
        public bool CanGenerateCertRequest
        {
            get
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Generates a certificate signing request for use with Xbox Live.
        /// </summary>
        public string GenerateCertRequest()
        {
            this.EnsureAdmin();

            CX509CertificateRequestCertificate certRequest = new CX509CertificateRequestCertificate();
            certRequest.Initialize(X509CertificateEnrollmentContext.ContextMachine);
            certRequest.PrivateKey.Length = 2048;
            certRequest.PrivateKey.ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider";
            certRequest.PrivateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;

            CX500DistinguishedName subject = new CX500DistinguishedName();
            subject.Encode("CN=NOT USED");
            certRequest.Subject = subject;

            CX509Enrollment enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(certRequest);
            enroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64HEADER);
            
            this.privateKey = certRequest.PrivateKey.Export("PRIVATEBLOB", EncodingType.XCN_CRYPT_STRING_BASE64).ToSecureString();
            return certRequest.PrivateKey.Export("PUBLICBLOB", EncodingType.XCN_CRYPT_STRING_BASE64 | EncodingType.XCN_CRYPT_STRING_NOCRLF);
        }

        /// <summary>
        /// Exports a PFX certificate with a given password.
        /// </summary>
        /// <param name="certificate">The certificate returned from Xbox Live from the associated signing request.</param>
        /// <param name="password">The password to secure the password with.</param>
        public byte[] ExportPfx(byte[] certificate, SecureString password)
        {
            if (this.privateKey == null)
            {
                throw new InvalidOperationException("There is no private key associated with this certificate. You must generate a request first.");
            }

            X509Certificate2 cert = new X509Certificate2(certificate);
            CspParameters csp = new CspParameters
            {
                KeyContainerName = "KeyContainer"
            };

            RSACryptoServiceProvider rsaPrivate = new RSACryptoServiceProvider(csp);
            rsaPrivate.ImportCspBlob(Convert.FromBase64String(this.privateKey.FromSecureString()));
            cert.PrivateKey = rsaPrivate;
            return cert.Export(X509ContentType.Pfx, password);
        }

        private void EnsureAdmin()
        {
            if (!this.CanGenerateCertRequest)
            {
                throw new SecurityException("Current user must be an administrator to create a certificate request.");
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="CertificateHelper"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="CertificateHelper"/>.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.privateKey != null)
                {
                    this.privateKey.Dispose();
                    this.privateKey = null;
                }
            }
        }
    }
}
