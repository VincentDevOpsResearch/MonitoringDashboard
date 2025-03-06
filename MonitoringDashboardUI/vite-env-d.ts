/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_API_BASE_URL: String;
  }
  
  interface ImportMeta {
    readonly env: ImportMetaEnv;
  }
  