using System.Net.Http.Headers;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Core.API
{
    public class ProgressHandler : HttpClientHandler
    {
        private readonly AttachmentProgress _attachmentProgress;
        private readonly Func<string> _signedNonceFunc;

        public ProgressHandler(AttachmentProgress attachmentProgress, Func<string> signedNonceFunc)
        {
            _attachmentProgress = attachmentProgress;
            _signedNonceFunc = signedNonceFunc;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(CryptoConstants.NONCE_SCHEME, _signedNonceFunc());

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Content != null)
            {
                var contentLength = response.Content.Headers.ContentLength ?? -1L;
                var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                var progressStream = new ProgressStream(responseStream, contentLength, _attachmentProgress);
                response.Content = new StreamContent(progressStream);

                foreach (var header in response.Content.Headers)
                {
                    response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return response;
        }
    }

}
