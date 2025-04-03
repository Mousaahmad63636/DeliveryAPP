using System;
using System.Threading.Tasks;
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class CurrencyService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CurrencyService> _logger;
        private CurrencySetting _currencySettings;

        public CurrencyService(AppDbContext dbContext, ILogger<CurrencyService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads currency settings from the database.
        /// </summary>
        public async Task LoadSettingsAsync()
        {
            try
            {
                _currencySettings = await _dbContext.CurrencySettings.FirstOrDefaultAsync();

                if (_currencySettings == null)
                {
                    // Create default settings if none exist
                    _currencySettings = new CurrencySetting
                    {
                        EnableMultipleCurrencies = true,
                        EnableUSD = true,
                        EnableLBP = true,
                        USDToLBPRate = 90000M,
                        DefaultCurrency = "USD",
                        LastUpdated = DateTime.Now
                    };

                    _dbContext.CurrencySettings.Add(_currencySettings);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading currency settings");
                throw;
            }
        }

        /// <summary>
        /// Gets the current currency settings.
        /// </summary>
        /// <returns>The current currency settings</returns>
        public async Task<CurrencySetting> GetSettingsAsync()
        {
            if (_currencySettings == null)
            {
                await LoadSettingsAsync();
            }

            return _currencySettings;
        }

        /// <summary>
        /// Updates the currency settings.
        /// </summary>
        /// <param name="settings">The updated settings</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateSettingsAsync(CurrencySetting settings)
        {
            try
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));

                var existing = await _dbContext.CurrencySettings.FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Update existing
                    existing.EnableMultipleCurrencies = settings.EnableMultipleCurrencies;
                    existing.EnableUSD = settings.EnableUSD;
                    existing.EnableLBP = settings.EnableLBP;
                    existing.USDToLBPRate = settings.USDToLBPRate;
                    existing.DefaultCurrency = settings.DefaultCurrency;
                    existing.LastUpdated = DateTime.Now;

                    _dbContext.CurrencySettings.Update(existing);
                }
                else
                {
                    // Create new
                    settings.LastUpdated = DateTime.Now;
                    _dbContext.CurrencySettings.Add(settings);
                }

                await _dbContext.SaveChangesAsync();

                // Update local cache
                _currencySettings = settings;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating currency settings");
                return false;
            }
        }

        /// <summary>
        /// Converts an amount from one currency to another.
        /// </summary>
        /// <param name="amount">The amount to convert</param>
        /// <param name="fromCurrency">The source currency (USD or LBP)</param>
        /// <param name="toCurrency">The target currency (USD or LBP)</param>
        /// <returns>The converted amount</returns>
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

        /// <summary>
        /// Gets the current exchange rate.
        /// </summary>
        /// <returns>The current USD to LBP exchange rate</returns>
        public async Task<decimal> GetExchangeRateAsync()
        {
            if (_currencySettings == null)
            {
                await LoadSettingsAsync();
            }

            return _currencySettings.USDToLBPRate;
        }

        /// <summary>
        /// Updates the exchange rate.
        /// </summary>
        /// <param name="newRate">The new exchange rate</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateExchangeRateAsync(decimal newRate)
        {
            try
            {
                if (newRate <= 0)
                    throw new ArgumentException("Exchange rate must be greater than zero");

                if (_currencySettings == null)
                {
                    await LoadSettingsAsync();
                }

                _currencySettings.USDToLBPRate = newRate;
                _currencySettings.LastUpdated = DateTime.Now;

                _dbContext.CurrencySettings.Update(_currencySettings);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange rate");
                return false;
            }
        }

        /// <summary>
        /// Formats a monetary value with the appropriate currency symbol.
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <param name="currency">The currency code (USD or LBP)</param>
        /// <returns>Formatted currency string</returns>
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