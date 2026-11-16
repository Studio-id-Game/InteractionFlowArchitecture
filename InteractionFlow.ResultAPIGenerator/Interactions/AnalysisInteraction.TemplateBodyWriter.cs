using System;
using System.Collections.Generic;
using System.Text;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed partial class AnalysisInteraction
{
    private readonly struct TemplateCall(string expression, string? awaitedExpression = null, bool forwardsReceiver = false)
    {
        public string Expression { get; } = expression;
        public string? AwaitedExpression { get; } = awaitedExpression;
        public bool ForwardsReceiver { get; } = forwardsReceiver;
    }

    private sealed class TemplateBodyWriter(
        TemplatePlaceholders placeholders,
        IParameterSymbol receiver,
        IReadOnlyDictionary<IInvocationOperation, TemplateCall> calls,
        bool awaitCallbacks)
    {
        private readonly StringBuilder source = new();

        public string? Write(IOperation body) => WriteStatement(body, 0) ? source.ToString() : null;

        private bool WriteStatement(IOperation operation, int depth)
        {
            switch (operation)
            {
                case IBlockOperation block:
                    Line(depth, "{");
                    foreach (var statement in block.Operations)
                    {
                        if (!WriteStatement(statement, depth + 1))
                            return false;
                    }
                    Line(depth, "}");
                    return true;
                case IConditionalOperation branch when WriteExpression(branch.Condition) is { } condition:
                    Line(depth, $"if ({condition})");
                    if (!WriteBranch(branch.WhenTrue, depth))
                        return false;
                    if (branch.WhenFalse is null)
                        return true;
                    Line(depth, "else");
                    return WriteBranch(branch.WhenFalse, depth);
                case IReturnOperation { ReturnedValue: null }:
                    Line(depth, "return;");
                    return true;
                case IReturnOperation { ReturnedValue: { } value } when WriteExpression(value) is { } expression:
                    Line(depth, $"return {expression};");
                    return true;
                case IExpressionStatementOperation { Operation: IInvocationOperation call }
                    when calls.TryGetValue(call, out var resolved) && resolved.ForwardsReceiver:
                    return true;
                case IExpressionStatementOperation statement when WriteExpression(statement.Operation) is { } expression:
                    Line(depth, expression + ";");
                    return true;
                default:
                    return false;
            }
        }

        private bool WriteBranch(IOperation branch, int depth)
        {
            if (branch is IBlockOperation)
                return WriteStatement(branch, depth);

            Line(depth, "{");
            var success = WriteStatement(branch, depth + 1);
            Line(depth, "}");
            return success;
        }

        private string? WriteExpression(IOperation operation) => operation switch
        {
            IParameterReferenceOperation parameter when IsReceiver(parameter) => TypeNames.Escape(receiver.Name),
            IPropertyReferenceOperation { Instance: IParameterReferenceOperation instance } property
                when IsReceiver(instance) && placeholders.IsSuccessProperty(property.Property)
                => TypeNames.Escape(receiver.Name) + ".IsSuccess",
            IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, OperatorMethod: null } not
                when WriteExpression(not.Operand) is { } operand => $"!({operand})",
            IInvocationOperation call when calls.TryGetValue(call, out var resolved)
                => awaitCallbacks ? resolved.AwaitedExpression ?? resolved.Expression : resolved.Expression,
            _ => null,
        };

        private bool IsReceiver(IParameterReferenceOperation parameter) =>
            SymbolEqualityComparer.Default.Equals(parameter.Parameter, receiver);

        private void Line(int depth, string value) => source.Append(' ', depth * 4).Append(value).Append('\n');
    }
}
