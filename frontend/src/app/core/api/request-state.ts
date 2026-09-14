import { catchError, map, Observable, of, startWith } from "rxjs";
import { apiErrorMessage } from "../errors/api-error";

export interface RequestState<T> {
  readonly value: T;
  readonly loading: boolean;
  readonly error: string;
}

/** Maps HTTP lifecycle to UI state; e.g. a failed list exposes an error and an empty value. */
export function requestState<T>(
  request: Observable<T>,
  emptyValue: T,
): Observable<RequestState<T>> {
  const initial = { value: emptyValue, loading: true, error: "" };
  return request.pipe(
    map((value) => ({ value, loading: false, error: "" })),
    catchError((error: unknown) =>
      of({ value: emptyValue, loading: false, error: apiErrorMessage(error) }),
    ),
    startWith(initial),
  );
}
