using System;
using System.Threading.Tasks;
using AcmeDriver.Certificates;

namespace AcmeDriver {
	public class AcmeOrdersClient : IAcmeOrdersClient{

		private readonly AcmeAuthenticatedClientContext _context;

		public AcmeOrdersClient(AcmeAuthenticatedClientContext context) {
			_context = context;
		}

		public Task<AcmeOrder> GetOrderAsync(Uri location) {
			return _context.SendPostAsGetAsync<AcmeOrder>(location);
		}

		public async Task<AcmeOrder> NewOrderAsync(AcmeOrder order) {
			return await _context.SendPostKidAsync<object, AcmeOrder>(_context.Directory.NewOrderUrl, new {
				identifiers = order.Identifiers,
			}, (headers, ord) => {
				ord.Location = headers.Location ?? order.Location;
			}).ConfigureAwait(false);
		}

		public async Task<AcmeOrder> FinalizeOrderAsync(AcmeOrder order, string csr) {
			return await _context.SendPostKidAsync<object, AcmeOrder>(order.Finalize, new {
				csr = Base64Url.Encode(csr.GetPemCsrData())
			}, (headers, ord) => {
				ord.Location = headers.Location ?? order.Location;
			}).ConfigureAwait(false);
		}

		public Task<string> DownloadCertificateAsync(AcmeOrder order) {
			if (order.Certificate == null) {
				throw new ArgumentException("Order's certificate field is null");
			}
			return _context.SendPostAsGetStringAsync(order.Certificate);
		}

		public async Task RevokeCertificateAsync(AcmeOrder order, CertificateRevokeReason reason) {
            var certificatePem = await DownloadCertificateAsync(order);
            var model = new CertificateRevokeModel {
                Certificate = PemUtils.GetEncodedCertFromPem(certificatePem),
                Reason = reason
            };
            await _context.SendPostVoidAsync(_context.Directory.RevokeCertUrl, model);
        }

	}
}