using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Feedback.Api.Events;

public record FeedbackSubmittedSignal(string Comment, int Rating) : IEvent;



