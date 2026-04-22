using System.Threading.Tasks;

namespace DocuSignPoc.Services.Foundations.QuickBooks;

public interface IQuickBooksService
{
    ValueTask ProcessInvoiceAsync(string invoiceId);
}
