using IotaLambda.Intersection;

namespace SampleApp;



[IntersectionType]
internal readonly partial struct EShopBankShape : IPayWithCreditCard, IPayWithInvoice;



internal enum PaymentMethod { CreditCard, Invoice }
internal class EShop
{
    public void Buy(PaymentMethod paymentMethod, EShopBankShape bank)
    {
        switch (paymentMethod)
        {
            case PaymentMethod.CreditCard:
                bank.PayWithCreditCard("CUSTOMER_CARD_NUMBER", 420M);
                break;

            case PaymentMethod.Invoice:
                var invoiceNumber = bank.PayWithInvoice("CUSTOMER_ADDRESS", 420M);
                Console.WriteLine($"Invoice number = {invoiceNumber}");
                break;
        }
    }
}
