using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orckestra.Composer.Providers;
using Orckestra.Composer.ViewModels;
using Orckestra.Overture.ServiceModel.RecurringOrders;

namespace Orckestra.Composer.Factory
{
    public class RecurringOrderProgramViewModelFactory : IRecurringOrderProgramViewModelFactory
    {
        protected IViewModelMapper ViewModelMapper { get; private set; }
        protected ILocalizationProvider LocalizationProvider { get; private set; }

        public RecurringOrderProgramViewModelFactory(
            IViewModelMapper viewModelMapper,
            ILocalizationProvider localizationProvider)
        {
            ViewModelMapper = viewModelMapper;
            LocalizationProvider = localizationProvider;
        }

        public virtual RecurringOrderProgramViewModel CreateRecurringOrderProgramViewModel(RecurringOrderProgram program, CultureInfo culture)
        {
            if (program == null) { throw new ArgumentNullException(nameof(program)); }
            if (culture == null) { throw new ArgumentNullException(nameof(culture)); }

            var vm = ViewModelMapper.MapTo<RecurringOrderProgramViewModel>(program, culture);

            if (vm == null) { return null; }

            var programLocalized = program.Localizations?.Find(l => string.Equals(l.CultureIso, culture.Name, StringComparison.OrdinalIgnoreCase));

            if (programLocalized != null)
            {
                vm.DisplayName = programLocalized.DisplayName;

                if (program.Frequencies != null && program.Frequencies.Any())
                {
                    Dictionary<string, RecurringOrderFrequency> dictionary = new Dictionary<string, RecurringOrderFrequency>(StringComparer.OrdinalIgnoreCase);
                    foreach (var frequency in program.Frequencies)
                    {
                        if (dictionary.ContainsKey(frequency.RecurringOrderFrequencyName))
                        {
                            continue;
                        }
                        dictionary.Add(frequency.RecurringOrderFrequencyName, frequency);
                    }
                    foreach(var vmFrequency in vm.Frequencies)
                    {
                        dictionary.TryGetValue(vmFrequency.RecurringOrderFrequencyName, out RecurringOrderFrequency match);
                        if (match != null)
                        {
                            var localizlocalizedFrequency = match.Localizations.Find(l => string.Equals(l.CultureIso, culture.Name, StringComparison.OrdinalIgnoreCase));
                            vmFrequency.DisplayName = localizlocalizedFrequency?.DisplayName ?? vmFrequency.RecurringOrderFrequencyName;
                        }
                    }
                }
            }
            vm.Frequencies = vm.Frequencies.OrderBy(f => f.SequenceNumber).ToList();
            return vm;
        }
    }
}