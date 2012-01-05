#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.StreamingStorage;
using ServiceStack.Text;

namespace Snippets.MailQuarantine
{
    /// <summary>
    /// See ReadMe.markdown for more information
    /// </summary>
    sealed class MailQuarantine : IEnvelopeQuarantine
    {
        readonly SmtpHandlerCore _core;
        readonly IStreamingContainer _container;
        readonly MemoryQuarantine _quarantine = new MemoryQuarantine();

        public MailQuarantine(SmtpHandlerCore core, IStreamingRoot root)
        {
            _core = core;
            _container = root.GetContainer("sample-errors").Create();
        }



   

        IStreamingItem GetStreamingItem(DateTime dateTime, string envelopeId)
        {
            var file = string.Format("{0:yyyy-MM-dd-HH-mm}-{1}-engine.txt",
                dateTime,
                envelopeId.ToLowerInvariant());

            return _container.GetItem(file);
        }

        public bool TryToQuarantine(ImmutableEnvelope envelope, Exception ex)
        {
            var quarantined = _quarantine.TryToQuarantine(envelope, ex);


            try
            {
                var item = GetStreamingItem(envelope.CreatedOnUtc, envelope.EnvelopeId);
                var data = "";
                try
                {
                    data = item.ReadText();
                }
                catch (StreamingItemNotFoundException) { }

                var builder = new StringBuilder(data);
                if (builder.Length == 0)
                {
                    builder.AppendLine(envelope.PrintToString(o => o.SerializeAndFormat()));
                }

                builder.AppendLine("[Exception]");
                builder.AppendLine(DateTime.UtcNow.ToString());
                builder.AppendLine(ex.ToString());
                builder.AppendLine();

                var text = builder.ToString();
                item.WriteText(text);

                if (quarantined)
                    ReportFailure(text, envelope);
            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
            }

            return quarantined;
        }

        public void Quarantine(byte[] message, Exception ex)
        {
            try
            {
                var item = GetStreamingItem(DateTime.UtcNow, "null-" + Guid.NewGuid().ToString().ToLowerInvariant());

                var builder = new StringBuilder();
                builder.AppendLine("Deserialization problem");
                builder.Append(ex).AppendLine();
                builder.AppendLine("Data");
                builder.AppendLine(Convert.ToBase64String(message, Base64FormattingOptions.InsertLineBreaks));
                item.WriteText(builder.ToString());
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        public void TryRelease(ImmutableEnvelope context)
        {
            _quarantine.TryRelease(context);
        }

        void ReportFailure(string text, ImmutableEnvelope envelope)
        {
            var name = envelope.Items
                .Select(e => e.MappedType.Name)
                .Select(e => (e.Replace("Command", "")))
                .FirstOrDefault() ?? "N/A";

            var subject = string.Format("[Error]: Sample fails to '{0}'", name);

            var builder = new StringBuilder();
            builder.AppendFormat(
                @"<p>Dear Lokad.CQRS Developer,</p><p>Something just went horribly wrong - there is a problem that I can't resolve. Please check the error log <strong>ASAP</strong>.</p>
                    <p>Here are a few details to help you out:</p><pre><code>");
            // EncoderUtil.EncodeForPre(
            builder.AppendLine(text);

            builder.AppendFormat(
                "</code></pre><p>Sincerely,<br /> Lokad.CQRS AI</p>");

            var mail = new MailMessage()
                {
                    Body = builder.ToString(),
                    Subject = subject,
                    IsBodyHtml = true,
                };
            mail.To.Add(new MailAddress("contact@company.com", "Sample Support"));
            _core.Send(mail);
        }
    }
}