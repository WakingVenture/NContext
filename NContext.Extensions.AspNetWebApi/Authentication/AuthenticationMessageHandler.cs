﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationMessageHandler.cs">
//   Copyright (c) 2012
//
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//   documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//   the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
//   and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//   The above copyright notice and this permission notice shall be included in all copies or substantial portions 
//   of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
//   TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//   CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
//   DEALINGS IN THE SOFTWARE.
// </copyright>
//
// <summary>
//   Defines an HttpOperationHandler for authentication.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NContext.Extensions.AspNetWebApi.Authentication
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Filters;
    using System.Web.Http.Hosting;

    /// <summary>
    /// Defines an <see cref="ActionFilterAttribute"/> for authentication.
    /// </summary>
    public class AuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IEnumerable<IProvideResourceAuthentication> _AuthenticationProviders;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationMessageHandler"/> class.
        /// </summary>
        /// <param name="authenticationProviders">The authentication providers.</param>
        /// <remarks></remarks>
        public AuthenticationMessageHandler(IEnumerable<IProvideResourceAuthentication> authenticationProviders)
        {
            _AuthenticationProviders = authenticationProviders ?? Enumerable.Empty<IProvideResourceAuthentication>();
        }

        #endregion

        #region Overrides of DelegatingHandler

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>. The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="request">The HTTP request message to send to the server.</param><param name="cancellationToken">A cancellation token to cancel operation.</param><exception cref="T:System.ArgumentNullException">The <paramref name="request"/> was null.</exception>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _AuthenticationProviders
                .FirstOrDefault(p => p.CanAuthenticate(request)).ToMaybe()
                .Bind(provider => provider.Authenticate(request).ToMaybe())
                .Bind(principal =>
                    {
                        request.Properties[HttpPropertyKeys.UserPrincipalKey] = principal;
                        return base.SendAsync(request, cancellationToken).ToMaybe();
                    })
                .FromMaybe(Task<HttpResponseMessage>.Factory.StartNew(
                           () =>
                           {
                               return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                           }));
        }

        #endregion
    }
}