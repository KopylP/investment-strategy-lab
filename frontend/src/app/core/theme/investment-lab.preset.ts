import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const InvestmentLabPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}',
      100: '{indigo.100}',
      200: '{indigo.200}',
      300: '{indigo.300}',
      400: '{indigo.400}',
      500: '{indigo.500}',
      600: '{indigo.600}',
      700: '{indigo.700}',
      800: '{indigo.800}',
      900: '{indigo.900}',
      950: '{indigo.950}',
    },
    colorScheme: {
      light: {
        finance: { gain: '{green.700}', loss: '{red.600}', neutral: '{surface.500}', tax: '{amber.700}' },
      },
      dark: {
        finance: { gain: '{green.400}', loss: '{red.400}', neutral: '{surface.500}', tax: '{amber.400}' },
      },
    },
  },
});
