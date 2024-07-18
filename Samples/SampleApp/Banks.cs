namespace SampleApp;

class DecentBank : IPayWithCash, IPayWithInvoice
{
    public void PayWithCash(decimal amount) => Console.WriteLine($"Paid with cash. TOTAL = {amount}");
    public string PayWithInvoice(string invoiceAddress, decimal amount)
    {
        Console.WriteLine($"Paid with invoice. TOTAL = {amount}");
        return "INVOICE-123";
    }
}

class SuperBank : IPayWithCash, IPayWithCreditCard
{
    public void PayWithCash(decimal amount) => Console.WriteLine($"Paid with cash. TOTAL = {amount}");
    public void PayWithCreditCard(string cardNumber, decimal amount) => Console.WriteLine($"Paid with credit card. TOTAL = {amount}");
}

class HyperBank : IPayWithCash, IPayWithCreditCard, IPayWithInvoice
{
    public void PayWithCash(decimal amount) => Console.WriteLine($"Paid with cash. TOTAL = {amount}");
    public void PayWithCreditCard(string cardNumber, decimal amount) => Console.WriteLine($"Paid with credit card. TOTAL = {amount}");
    public string PayWithInvoice(string invoiceAddress, decimal amount)
    {
        Console.WriteLine($"Paid with invoice. TOTAL = {amount}");
        return "INVOICE-123";
    }
}