using QBFC16Lib;

namespace QB_Items_Lib
{
    public static class AppConfig
    {
        // Application name used for QuickBooks connection
        public const string QB_APP_NAME = "QBSync";

        // QuickBooks connection timeout in milliseconds (adjust as needed)
        public const int QB_CONNECTION_TIMEOUT = 30000;

        // QuickBooks API version (ensure compatibility with the QuickBooks version in use)
        public const string QB_API_VERSION = "16.0";

        // Log file path for debugging QuickBooks interactions
        public const string LOG_FILE_PATH = "logs/qb_sync.log";

        // Default country code for QuickBooks requests
        public const string QB_COUNTRY_CODE = "US";

        // Error handling mode for QuickBooks requests
        public const ENRqOnError QB_ERROR_HANDLING = ENRqOnError.roeContinue;
    }
}