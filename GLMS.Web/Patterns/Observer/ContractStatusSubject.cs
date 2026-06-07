using GLMS.Web.Models;

namespace GLMS.Web.Patterns.Observer
{
    /// <summary>
    /// OBSERVER PATTERN — Subject (Publisher)
    ///
    /// The ContractStatusSubject maintains a list of observers and notifies
    /// them all when a contract's status changes. It is injected as a scoped
    /// service in Program.cs so the ContractsController can use it.
    ///
    /// Flow:
    ///   ContractsController changes contract.Status
    ///   → calls subject.Notify(contractId, newStatus)
    ///   → all registered observers run their UpdateAsync()
    /// </summary>
    public class ContractStatusSubject
    {
        // PRESENTATION POINT: This is the subscriber list.
        // Think of it like a mailing list — anyone who "subscribes" gets notified.
        // Currently 2 subscribers: ServiceRequestObserver and FinanceObserver.
        // Adding a 3rd (e.g. EmailObserver) = just call Attach() in Program.cs. Zero other changes.
        private readonly List<IStatusObserver> _observers = new();

        // Attach = "subscribe to status change notifications"
        public void Attach(IStatusObserver observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        // Detach = "unsubscribe"
        public void Detach(IStatusObserver observer)
        {
            _observers.Remove(observer);
        }

        /// <summary>
        /// Notifies all registered observers of a status change.
        /// </summary>
        public async Task NotifyAsync(int contractId, ContractStatus newStatus)
        {
            // PRESENTATION POINT: We loop through EVERY subscriber and call UpdateAsync().
            // The Subject doesn't know what the observers do — that's their own business.
            // ServiceRequestObserver cancels pending requests.
            // FinanceObserver logs a finance alert.
            // Both happen automatically just by changing a contract status in the Edit form.
            foreach (var observer in _observers)
            {
                await observer.UpdateAsync(contractId, newStatus);
            }
        }
    }
}
