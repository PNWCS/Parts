using QBFC16Lib;
using Serilog;

namespace QB_Items_Test
{
    public class QuickBooksSession : IDisposable
    {
        private readonly QBSessionManager _sessionManager; // Readonly as it is only set in the constructor
        private bool _sessionBegun; // Tracks if a session has started
        private bool _connectionOpen; // Tracks if a connection is open

        public QuickBooksSession(string appName)
        {
            _sessionManager = new QBSessionManager();

            try
            {
                _sessionManager.OpenConnection("", appName);
                _connectionOpen = true;

                _sessionManager.BeginSession("", ENOpenMode.omDontCare);
                _sessionBegun = true;

                Log.Debug("QuickBooks session successfully created.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing QuickBooks session.");
                CleanupPartialConnection();
                throw; // Re-throw to notify the caller
            }
        }

        public IMsgSetRequest CreateRequestSet()
        {
            try
            {
                var requestMsgSet = _sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;
                return requestMsgSet;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating request message set.");
                throw;
            }
        }

        public IMsgSetResponse SendRequest(IMsgSetRequest requestMsgSet)
        {
            try
            {
                return _sessionManager.DoRequests(requestMsgSet);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending request to QuickBooks.");
                throw;
            }
        }

        public void Dispose()
        {
            CleanupSession();
            CleanupConnection();
            GC.SuppressFinalize(this); // Suppress finalization for this object
        }

        private void CleanupSession()
        {
            if (_sessionBegun)
            {
                try
                {
                    _sessionManager.EndSession();
                    Log.Debug("QuickBooks session ended successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error ending QuickBooks session.");
                }
                finally
                {
                    _sessionBegun = false;
                }
            }
        }

        private void CleanupConnection()
        {
            if (_connectionOpen)
            {
                try
                {
                    _sessionManager.CloseConnection();
                    Log.Debug("QuickBooks connection closed successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error closing QuickBooks connection.");
                }
                finally
                {
                    _connectionOpen = false;
                }
            }
        }

        private void CleanupPartialConnection()
        {
            if (_connectionOpen)
            {
                try
                {
                    _sessionManager.CloseConnection();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error during partial connection cleanup.");
                }
                finally
                {
                    _connectionOpen = false;
                }
            }
        }
    }
}