export function requireApiData<T>(data: T | null, errorMessage = "The server returned an empty response.") {
  if (data === null) {
    throw new Error(errorMessage);
  }

  return data;
}
