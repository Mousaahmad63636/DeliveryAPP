using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class CurrencyService : BaseService
    {
        private CurrencySetting _currencySettings;

        public CurrencyService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<CurrencyService> logger)
            : base(dbContextFactory, logger)
        {
        }

        public async Task LoadSettingsAsync()
        {
            await ExecuteDbOperationAsync(async (dbContext) =>
            {
                _currencySettings = await dbContext.CurrencySettings.FirstOrDefaultAsync();

                if (_currencySettings == null)
                {
                    _currencySettings = new CurrencySetting
                    {
                        EnableMultipleCurrencies = true,
                        EnableUSD = true,
                        EnableLBP = true,
                        USDToLBPRate = 90000M,
                        DefaultCurrency = "USD",
                        LastUpdated = DateTime.Now
                    };

                    dbContext.CurrencySettings.Add(_currencySettings);
                    await dbContext.SaveChangesAsync();
                }
            });
        }

        public async Task<CurrencySetting> GetSettingsAsync()
        {
            if (_currencySettings == null)
            {
                await LoadSettingsAsync();
            }

            return _currencySettings;
        }

        public async Task<bool> UpdateSettingsAsync(CurrencySetting settings)
        {
            try
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));

                return await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    var existing = await dbContext.CurrencySettings.FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        existing.EnableMultipleCurrencies = settings.EnableMultipleCurrencies;
                        existing.EnableUSD = settings.EnableUSD;
                        existing.EnableLBP = settings.EnableLBP;
                        existing.USDToLBPRate = settings.USDToLBPRate;
                        existing.DefaultCurrency = settings.DefaultCurrency;
                        existing.LastUpdated = DateTime.Now;

                        dbContext.CurrencySettings.Update(existing);
                    }
                    else
                    {
                        settings.LastUpdated = DateTime.Now;
                        dbContext.CurrencySettings.Add(settings);
                    }

                    await dbContext.SaveChangesAsync();

                    _currencySettings = settings;
                    return true;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating currency settings");
                return false;
            }
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency)
                return amount;

            if (_currencySettings == null)
            {
                await LoadSettingsAsync();
            }

            if (fromCurrency.ToUpper() == "USD" && toCurrency.ToUpper() == "LBP")
            {
                return amount * _currencySettings.USDToLBPRate;
            }
            else if (fromCurrency.ToUpper() == "LBP" && toCurrency.ToUpper() == "USD")
            {
                return amount / _currencySettings.USDToLBPRate;
            }

            throw new ArgumentException($"Unsupported currency conversion: {fromCurrency} to {toCurrency}");
        }

        public async Task<decimal> GetExchangeRateAsync()
        {
            if (_currencySettings == null)
            {
                await LoadSettingsAsync();
            }

            return _currencySettings.USDToLBPRate;
        }

        public async Task<bool> UpdateExchangeRateAsync(decimal newRate)
        {
            try
            {
                if (newRate <= 0)
                    throw new ArgumentException("Exchange rate must be greater than zero");

                return await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    if (_currencySettings == null)
                    {
                        await LoadSettingsAsync();
                    }

                    var settings = await dbContext.CurrencySettings.FirstOrDefaultAsync();
                    if (settings != null)
                    {
                        settings.USDToLBPRate = newRate;
                        settings.LastUpdated = DateTime.Now;
                        await dbContext.SaveChangesAsync();

                        _currencySettings.USDToLBPRate = newRate;
                        _currencySettings.LastUpdated = DateTime.Now;
                        return true;
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating exchange rate");
                return false;
            }
        }

        public string FormatCurrency(decimal amount, string currency)
        {
            if (currency.ToUpper() == "USD")
            {
                return $"${amount:N2}";
            }
            else if (currency.ToUpper() == "LBP")
            {
                return $"{amount:N0} ل.ل.";
            }

            return $"{amount:N2} {currency}";
        }
    }
}