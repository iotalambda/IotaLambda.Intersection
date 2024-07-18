using SampleApp;

var eshop = new EShop();

/**
 * ERROR: The type 'SampleApp.DecentBank' cannot be used as type parameter 'T' in the generic type
 *        or method 'EShopBankShape.From<T>(T, object)'. There is no implicit reference conversion
 *        from 'SampleApp.DecentBank' to 'SampleApp.IPayWithCreditCard'.
 */
//eshop.Buy(PaymentMethod.CreditCard, EShopBankShape.From(new DecentBank()))

/**
 * ERROR: The type 'SampleApp.SuperBank' cannot be used as type parameter 'T' in the generic type
 *        or method 'EShopBankShape.From<T>(T, object)'. There is no implicit reference conversion
 *        from 'SampleApp.SuperBank' to 'SampleApp.IPayWithInvoice'.
 */
//eshop.Buy(PaymentMethod.CreditCard, EShopBankShape.From(new SuperBank()));

// OK
eshop.Buy(PaymentMethod.CreditCard, EShopBankShape.From(new HyperBank()));