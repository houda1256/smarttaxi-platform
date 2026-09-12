using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetOpenSessionForRegister;

public sealed record GetOpenSessionForRegisterQuery(Guid CashRegisterId) : IQuery<CashRegisterSession?>;
