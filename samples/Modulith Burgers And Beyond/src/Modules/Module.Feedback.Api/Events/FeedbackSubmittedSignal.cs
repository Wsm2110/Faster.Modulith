using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Feedback.Api.Events;

public record FeedbackSubmittedSignalEvent(string Comment, int Rating) : IEvent;



