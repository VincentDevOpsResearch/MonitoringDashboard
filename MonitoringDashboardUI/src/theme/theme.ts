import { createTheme, ThemeProvider } from '@mui/material/styles';
import { red, blue } from '@mui/material/colors';

const theme = createTheme({
  palette: {
    primary: {
      main: blue[500], // Normal
    },
    error: {
      main: red[500],  // Alert
    },
  },
});

export default theme;
