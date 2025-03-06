const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5292/api';

interface FetchOptions {
  path: string;
  method?: string;
  data?: any;
  params?: Record<string, any>;
}

export const fetchData = async <T = any>({
  path,
  method = 'GET',
  data,
  params,
}: FetchOptions): Promise<T> => {
  const queryString = params
    ? '?' +
      Object.entries(params)
        .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
        .join('&')
    : '';

  const url = `${baseUrl}${path}${queryString}`;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 seconds timeout

  const fetchOptions: RequestInit = {
    method,
    headers: {
      'Content-Type': 'application/json',
      // Add authorization header if required
      Authorization: `Bearer ${localStorage.getItem('token') || ''}`,
    },
    signal: controller.signal,
  };

  if (data) {
    fetchOptions.body = JSON.stringify(data);
  }

  try {
    console.log(`Fetching ${url} with method: ${method}`);
    const response = await fetch(url, fetchOptions);
    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const contentType = response.headers.get('Content-Type') || '';
    if (contentType.includes('application/json')) {
      return response.json();
    } else if (contentType.includes('text/')) {
      return response.text() as T;
    } else {
      return response.blob() as T;
    }
  } catch (error) {
    console.error(`Error fetching ${url}:`, error);
    throw error;
  }
};

interface streamFetchOptions {
  method?: string;
  data?: any;
  params?: Record<string, string>;
  signal?: AbortSignal;
}

export const fetchStream = async (
  path: string,
  onData: (chunk: string) => void,
  options: streamFetchOptions = {}
) => {
  const queryString = options.params
    ? '?' +
      Object.entries(options.params)
        .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
        .join('&')
    : '';

  const url = `${baseUrl}${path}${queryString}`;

  const controller = new AbortController();
  const signal = options.signal || controller.signal; 
  const timeoutId = setTimeout(() => controller.abort(), 10000); 

  const fetchOptions: RequestInit = {
    method: options.method || 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    signal,
  };

  try {
    const response = await fetch(url, fetchOptions);
    clearTimeout(timeoutId); 

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error('ReadableStream not supported.');
    }

    const decoder = new TextDecoder();

    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      const chunk = decoder.decode(value);
      onData(chunk); 
    }
  } catch (error) {
    console.error(`Error streaming ${url}:`, error);
    throw error;
  } finally {
    clearTimeout(timeoutId); // Ensure clean up
  }
};
