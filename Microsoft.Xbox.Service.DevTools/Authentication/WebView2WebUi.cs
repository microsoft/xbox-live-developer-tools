// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensibility;
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.WinForms;
    using Microsoft.Xbox.Services.DevTools.Common;

    /// <summary>
    /// Shows MSAL's interactive sign in in WebView2. MSAL's own embedded dialog uses the legacy
    /// Internet Explorer control for login.microsoftonline.com authorities, which no longer renders the sign in page.
    /// </summary>
    internal class WebView2WebUi : ICustomWebUi
    {
        private const string WebView2RuntimeUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

        private readonly string profileName;
        private readonly bool allowSingleSignOn;

        /// <param name="profileName">Browser data folder name, so different account types don't share cookies.</param>
        /// <param name="allowSingleSignOn">Whether to sign in with the Windows primary account when possible.</param>
        public WebView2WebUi(string profileName, bool allowSingleSignOn)
        {
            this.profileName = profileName;
            this.allowSingleSignOn = allowSingleSignOn;
        }

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

            // WinForms and WebView2 need a single-threaded apartment with its own message loop.
            var thread = new Thread(() => this.RunSignInWindow(authorizationUri, redirectUri, completion, cancellationToken))
            {
                IsBackground = true,
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return completion.Task;
        }

        private static bool IsRedirect(Uri uri, Uri redirectUri)
        {
            return string.Equals(uri.Scheme, redirectUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.Authority, redirectUri.Authority, StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.AbsolutePath.TrimEnd('/'), redirectUri.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        private void RunSignInWindow(Uri authorizationUri, Uri redirectUri, TaskCompletionSource<Uri> completion, CancellationToken cancellationToken)
        {
            try
            {
                using (var form = new Form())
                using (var webView = new WebView2())
                {
                    CancellationTokenRegistration cancellation = default(CancellationTokenRegistration);

                    form.Text = "Sign in";
                    form.ClientSize = new Size(566, 640);
                    form.StartPosition = FormStartPosition.CenterScreen;
                    form.ShowIcon = false;
                    form.MinimizeBox = false;
                    form.MaximizeBox = false;
                    webView.Dock = DockStyle.Fill;
                    form.Controls.Add(webView);

                    webView.NavigationStarting += (sender, e) =>
                    {
                        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri uri))
                        {
                            return;
                        }

                        if (IsRedirect(uri, redirectUri))
                        {
                            e.Cancel = true;
                            completion.TrySetResult(uri);
                            form.Close();
                        }
                        else if (uri.Scheme != Uri.UriSchemeHttps)
                        {
                            e.Cancel = true;
                            completion.TrySetException(new MsalClientException(
                                "non_https_redirect_failed",
                                $"Sign in was stopped because it tried to open a page that is not HTTPS: {uri.GetLeftPart(UriPartial.Path)}"));
                            form.Close();
                        }
                    };

                    form.Load += async (sender, e) =>
                    {
                        cancellation = cancellationToken.Register(() =>
                        {
                            completion.TrySetCanceled(cancellationToken);
                            form.BeginInvoke(new Action(form.Close));
                        });

                        try
                        {
                            var options = new CoreWebView2EnvironmentOptions
                            {
                                AllowSingleSignOnUsingOSPrimaryAccount = this.allowSingleSignOn,
                            };
                            string userDataFolder = Path.Combine(ClientSettings.Singleton.CacheFolder, "WebView2", this.profileName);
                            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                            await webView.EnsureCoreWebView2Async(environment);

                            webView.CoreWebView2.DocumentTitleChanged += (s, args) => form.Text = webView.CoreWebView2.DocumentTitle;

                            // Sign in doesn't need device access, so deny camera and similar requests without prompting.
                            webView.CoreWebView2.PermissionRequested += (s, args) => args.State = CoreWebView2PermissionState.Deny;
                            webView.CoreWebView2.Navigate(authorizationUri.AbsoluteUri);
                        }
                        catch (WebView2RuntimeNotFoundException ex)
                        {
                            completion.TrySetException(new InvalidOperationException(
                                $"Signing in requires the Microsoft Edge WebView2 Runtime. Install it from {WebView2RuntimeUrl} and try again.",
                                ex));
                            form.Close();
                        }
                        catch (Exception ex)
                        {
                            completion.TrySetException(ex);
                            form.Close();
                        }
                    };

                    // Console tools have no owner window, so bring the sign in window to the front.
                    form.Shown += (sender, e) =>
                    {
                        form.TopMost = true;
                        form.Activate();
                        form.TopMost = false;
                    };

                    form.FormClosed += (sender, e) =>
                    {
                        cancellation.Dispose();
                        completion.TrySetException(new MsalClientException(
                            MsalError.AuthenticationCanceledError,
                            "The sign in window was closed before sign in completed."));
                    };

                    Application.Run(form);
                }
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }
    }
}
