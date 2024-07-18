namespace SampleApp;

interface IPayWithCash
{
    void PayWithCash(decimal amount);
}

interface IPayWithInvoice
{
    string PayWithInvoice(string invoiceAddress, decimal amount);
}

interface IPayWithCreditCard
{
    void PayWithCreditCard(string cardNumber, decimal amount);
}
