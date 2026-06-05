using System;
using System.Threading;

namespace GlueFramework.Core.Services
{
    public static class EventDispatchContext
    {
        private static readonly AsyncLocal<Guid?> _messageId = new AsyncLocal<Guid?>();

        public static Guid? MessageId
        {
            get => _messageId.Value;
            set => _messageId.Value = value;
        }
    }
}
