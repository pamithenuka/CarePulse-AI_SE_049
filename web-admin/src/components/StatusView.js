export function LoadingState({ label = "Loading..." }) {
  return (
    <div className="status-view" role="status">
      {label}
    </div>
  );
}

export function ErrorState({ message = "Something went wrong.", onRetry }) {
  return (
    <div className="status-view status-view-error" role="alert">
      <p>{message}</p>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}

export function EmptyState({ message = "No records found." }) {
  return <div className="status-view">{message}</div>;
}
