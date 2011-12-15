#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Snippets.HttpEndpoint
{
    public class MouseStats
    {
        public int MessagesCount { get; set; }
        public int MessagesPerSecond { get; set; }
        public long Distance { get; set; }

        int _messageCounter;
        int _currentSecond;

        public void RecordMessage()
        {
            var second = DateTime.Now.Second;

            if (second == _currentSecond)
            {
                _messageCounter += 1;
            }
            else
            {
                _currentSecond = second;
                MessagesPerSecond = _messageCounter;
                _messageCounter = 1;
            }
        }

        public void RefreshStatistics()
        {
            var second = DateTime.Now.Second;
            if (second == _currentSecond) return;

            _currentSecond = second;
            MessagesPerSecond = _messageCounter;
            _messageCounter = 0;

        }
    }
}