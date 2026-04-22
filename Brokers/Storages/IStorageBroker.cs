using System.Linq;
using DocuSignPoc.Models.Foundations.SignatureRequests;

namespace DocuSignPoc.Brokers.Storages;

public partial interface IStorageBroker
{
    IQueryable<SignatureRequest> SelectAllSignatureRequests();
    ValueTask<SignatureRequest> InsertSignatureRequestAsync(SignatureRequest signatureRequest);
    ValueTask<SignatureRequest> UpdateSignatureRequestAsync(SignatureRequest signatureRequest);
    ValueTask<SignatureRequest> DeleteSignatureRequestAsync(SignatureRequest signatureRequest);
}
