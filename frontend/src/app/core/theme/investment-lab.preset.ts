import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/** Brand and finance tokens; components take their look from here, never from their own CSS (§11.4). */
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
  },
  extend: {
    finance: {
      gain: { light: '{green.600}', dark: '{green.400}' },
      loss: { light: '{red.600}', dark: '{red.400}' },
      neutral: '{surface.500}',
      tax: '{amber.600}',
    },
  },
});
