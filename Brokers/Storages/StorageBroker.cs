using System;
using System.Linq;
using System.Threading.Tasks;
using DocuSignPoc.Models.Foundations.SignatureRequests;

namespace DocuSignPoc.Brokers.Storages;

public partial class StorageBroker : IStorageBroker
{
    private readonly SignatureRequestDbContext dbContext;

    public StorageBroker(SignatureRequestDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IQueryable<SignatureRequest> SelectAllSignatureRequests()
    {
        return this.dbContext.SignatureRequests.AsQueryable();
    }

    public async ValueTask<SignatureRequest> InsertSignatureRequestAsync(SignatureRequest signatureRequest)
    {
        await this.dbContext.SignatureRequests.AddAsync(signatureRequest);
        await this.dbContext.SaveChangesAsync();
        return signatureRequest;
    }

    public async ValueTask<SignatureRequest> UpdateSignatureRequestAsync(SignatureRequest signatureRequest)
    {
        this.dbContext.SignatureRequests.Update(signatureRequest);
        await this.dbContext.SaveChangesAsync();
        return signatureRequest;
    }

    public async ValueTask<SignatureRequest> DeleteSignatureRequestAsync(SignatureRequest signatureRequest)
    {
        this.dbContext.SignatureRequests.Remove(signatureRequest);
        await this.dbContext.SaveChangesAsync();
        return signatureRequest;
    }
}
