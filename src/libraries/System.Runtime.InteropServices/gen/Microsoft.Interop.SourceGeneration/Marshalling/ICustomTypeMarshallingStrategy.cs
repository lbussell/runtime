﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// The base interface for implementing various aspects of the custom native type and collection marshalling specs.
    /// </summary>
    internal interface ICustomTypeMarshallingStrategyBase
    {
        TypeSyntax AsNativeType(TypePositionInfo info);

        IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments);

        IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context);

        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }

    internal interface ICustomTypeMarshallingStrategy : ICustomTypeMarshallingStrategyBase
    {
        IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context);
        IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context);
    }

    /// <summary>
    /// Stateless marshalling support for a type that has a custom unmanaged type.
    /// </summary>
    internal sealed class StatelessValueMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;

        public StatelessValueMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.Free))
                yield break;

            // <marshallerType>.Free(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManagedGuaranteed(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManagedGuaranteed)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<ArgumentSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatelessCallerAllocatedBufferMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _marshallerType;
        private readonly TypeSyntax _bufferElementType;

        public StatelessCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
            _bufferElementType = bufferElementType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                string bufferIdentifier = context.GetAdditionalIdentifier(info, "buffer");

                // Span<bufferElementType> <bufferIdentifier> = stackalloc <bufferElementType>[<marshallerType>.BufferSize];
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        GenericName(
                            Identifier(TypeNames.System_Span),
                            TypeArgumentList(
                                SingletonSeparatedList(_bufferElementType))),
                        SingletonSeparatedList(
                            VariableDeclarator(bufferIdentifier)
                                .WithInitializer(EqualsValueClause(
                                    StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))
                                            ))))))))));

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>, <nativeIdentifier>__buffer);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(nativeIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerType,
                                IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(IdentifierName(bufferIdentifier))
                            })))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);

        private static bool CanUseCallerAllocatedBuffer(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
    }

    internal sealed class StatefulValueMarshalling : ICustomTypeMarshallingStrategy
    {
        internal const string MarshallerIdentifier = "marshaller";
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;

        public StatefulValueMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.Free))
                yield break;

            // <marshaller>.Free();
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList()));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManagedGuaranteed();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToManagedGuaranteed)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <marshaller>.FromManaged(<managedIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(managedIdentifier))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshaller>.ToUnmanaged();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToUnmanaged)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManaged();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToManaged)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged) && !_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <marshaller>.FromUnmanaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromUnmanaged)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(nativeIdentifier))))));
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<ArgumentSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return MarshallerHelpers.Declare(
                _marshallerTypeSyntax,
                context.GetAdditionalIdentifier(info, MarshallerIdentifier),
                ImplicitObjectCreationExpression(ArgumentList(), initializer: null));
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.NotifyInvokeSucceeded))
                yield break;

            // <marshaller>.NotifyInvokeSucceeded();
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.NotifyInvokeSucceeded)),
                    ArgumentList()));
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatefulCallerAllocatedBufferMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _marshallerType;
        private readonly TypeSyntax _bufferElementType;

        public StatefulCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
            _bufferElementType = bufferElementType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                // TODO: Update once we can consume the scoped keword. We should be able to simplify this once we get that API.
                string stackPtrIdentifier = context.GetAdditionalIdentifier(info, "stackptr");
                // <bufferElementType>* <managedIdentifier>__stackptr = stackalloc <bufferElementType>[<_bufferSize>];
                yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PointerType(_bufferElementType),
                    SingletonSeparatedList(
                        VariableDeclarator(stackPtrIdentifier)
                            .WithInitializer(EqualsValueClause(
                                StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))
                                            ))))))))));


                (string managedIdentifier, _) = context.GetIdentifiers(info);

                // <marshaller>.FromManaged(<managedIdentifier>);
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, StatefulValueMarshalling.MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged)),
                        ArgumentList(SeparatedList(
                            new[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(
                                    ObjectCreationExpression(
                                        GenericName(Identifier(TypeNames.System_Span),
                                            TypeArgumentList(SingletonSeparatedList(
                                                _bufferElementType))))
                                    .WithArgumentList(
                                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                                        {
                                            Argument(IdentifierName(stackPtrIdentifier)),
                                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize)))
                                        }))))
                            }))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        }

        private static bool CanUseCallerAllocatedBuffer(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<ArgumentSyntax>();
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
    }

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape, TypeSyntax managedElementType, TypeSyntax unmanagedElementType, ExpressionSyntax numElementsExpression)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
            _numElementsExpression = numElementsExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(managedIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                                .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                        })))));

            // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            ExpressionSyntax destination =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier)),
                    })));

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>).Clear();
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            destination,
                            IdentifierName("Clear"))));
                yield break;
            }

            // Skip the cast if the managed and unmanaged element types are the same
            if (!_unmanagedElementType.IsEquivalentTo(_managedElementType))
            {
                // MemoryMarshal.Cast<<unmanagedElementType>, <managedElementType>>(<destination>)
                destination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        GenericName(
                            Identifier("Cast"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList(
                                    new[]
                                    {
                                        _unmanagedElementType,
                                        _managedElementType
                                    })))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(destination))));
            }

            // <marshallerType>.GetManagedValuesSource(<managedIdentifer>).CopyTo(<destination>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier))))),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(GetNumElementsIdentifier(info, context)))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            ExpressionSyntax copySource;
            ExpressionSyntax copyDestination;
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
                copySource =
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier)),
                        })));

                // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<marshallerType>.GetManagedValuesSource(<managedIdentifer>)), <marshallerType>.GetManagedValuesSource(<managedIdentifer>).Length)
                copyDestination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        IdentifierName("CreateSpan")),
                    ArgumentList(
                        SeparatedList(new[]
                        {
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                        IdentifierName("GetReference")),
                                    ArgumentList(SingletonSeparatedList(
                                        Argument(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerTypeSyntax,
                                                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                                                ArgumentList(SingletonSeparatedList(
                                                    Argument(IdentifierName(managedIdentifier))))))))))
                                .WithRefKindKeyword(
                                    Token(SyntaxKind.RefKeyword)),
                            Argument(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            _marshallerTypeSyntax,
                                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName(managedIdentifier))))),
                                    IdentifierName("Length")))
                        })));

            }
            else
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(numElementsIdentifier),
                        _numElementsExpression));

                // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managedIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(nativeIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                            })))));

                // <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
                copySource = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource)),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier))
                    })));

                // <marshellerType>.GetManagedValuesDestination(<managedIdentifier>)
                copyDestination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination)),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier)))));
            }

            // Skip the cast if the managed and unmanaged element types are the same
            if (!_unmanagedElementType.IsEquivalentTo(_managedElementType))
            {
                // MemoryMarshal.Cast<<unmanagedElementType>, <elementType>>(<copySource>)
                copySource = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        GenericName(
                            Identifier("Cast"),
                            TypeArgumentList(SeparatedList(
                                new[]
                                {
                                    _unmanagedElementType,
                                    _managedElementType
                                })))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(copySource))));
            }

            // <copySource>.CopyTo(<copyDestination>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        copySource,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(copyDestination)));
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => Array.Empty<ArgumentSyntax>();

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        private static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
            => context.GetAdditionalIdentifier(info, "numElements");
    }
}
