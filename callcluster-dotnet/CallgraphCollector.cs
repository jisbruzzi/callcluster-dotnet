using System;
using System.Collections.Generic;
using System.Linq;
using callcluster_dotnet.dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace callcluster_dotnet
{
    internal class CallgraphCollector : ICallCollector, IMethodCollector, IClassCollector
    {
        private SymbolIndexer FunctionIndexer;
        private SemanticModel CurrentModel;
        private IList<(IMethodSymbol from, IMethodSymbol to, ITypeSymbol type)> Calls;

        /// <summary>
        /// A tree of overriden methods and how the methods override each other. The parent method is overriden by the child.
        /// </summary>
        private Tree<IMethodSymbol> MethodTree;
        /// <summary>
        /// A tree of inherited classes.
        /// </summary>
        private Tree<ITypeSymbol> ClassTree;
        private MethodLocator MethodLocator;
        private Solution CurrentSolution;
        private Project CurrentProject;

        internal void SetCurrent(Solution solution)
        {
            this.CurrentSolution = solution;
        }

        public CallgraphCollector()
        {
            this.FunctionIndexer = new SymbolIndexer();
            this.Calls = new List<(IMethodSymbol from, IMethodSymbol to, ITypeSymbol type)>();
            this.MethodTree = new Tree<IMethodSymbol>();
            this.ClassTree = new Tree<ITypeSymbol>();
            this.MethodLocator = new MethodLocator();
        }

        internal void SetCurrent(Project project)
        {
            this.CurrentProject = project;
        }

        public void AddMethod(ISymbol called)
        {
            FunctionIndexer.Add(called);
        }

        public void AddCall(IMethodSymbol caller, IMethodSymbol called, ITypeSymbol calledType)
        {
            this.Calls.Add((from:caller,to:called,type:calledType));
            FunctionIndexer.Add(called);
            FunctionIndexer.Add(caller);
        }

        internal CallgraphDTO GetCallgraphDTO()
        {
            return new CallgraphDTO(){
                functions = FunctionIndexer.GetFunctionDTOs(),
                calls = GetCallDTOs(),
                community = this.MethodLocator.GetCommunityDTO(FunctionIndexer)
            };
        }

        private IEnumerable<CallDTO> GetCallDTOs()
        {
            Console.WriteLine("Started listing all calls");
            return this.Calls.SelectMany((call)=>{
                long? from = FunctionIndexer.IndexOf(call.from);
                long? to = FunctionIndexer.IndexOf(call.to);

                if(!(from.HasValue && to.HasValue))
                {
                    return new List<CallDTO>();
                }
                else
                {
                    IEnumerable<IMethodSymbol> targetMethods = this.MethodTree.DescendantsOf(call.to);
                    IEnumerable<ITypeSymbol> descendantClasses = this.ClassTree.DescendantsOf(call.type);
                    IEnumerable<IMethodSymbol> filteredMethods = targetMethods.Where(s=>descendantClasses.Contains(s.ContainingType));

                    if(!filteredMethods.Contains(call.to)){
                        filteredMethods = filteredMethods.Append(call.to);
                    }

                    return filteredMethods.Select(s=>{
                        return new CallDTO(){
                            from = from.Value,
                            to = FunctionIndexer.IndexOf(s).Value
                        };
                    });

                }
            });
        }
        internal void SetModel(SemanticModel currentModel)
        {
            this.CurrentModel = currentModel;
        }

        public void AddClass(INamedTypeSymbol symbol)
        {
            if(symbol.BaseType != null)
            {
                ClassTree.Add(symbol.BaseType,symbol);
                if(symbol.BaseType != symbol){//object inherits from object
                    AddClass(symbol.BaseType);
                }
            }
            foreach(var @interface in symbol.Interfaces){
                ClassTree.Add(@interface, symbol);
                foreach(var abstractMethod in @interface.GetMembers().Where(m=>m is IMethodSymbol)){
                    var implementation = symbol.FindImplementationForInterfaceMember(abstractMethod);
                    this.AddMethod(abstractMethod as IMethodSymbol);
                    if(implementation!=null)
                    {
                        this.AddMethod(implementation as IMethodSymbol);
                        this.MethodTree.Add(abstractMethod as IMethodSymbol, implementation as IMethodSymbol);
                    }
                }
            }
        }

        private void AddOverrides(IMethodSymbol method)
        {
            if(method.OverriddenMethod != null)
            {
                FunctionIndexer.Add(method.OverriddenMethod);
                MethodTree.Add(method.OverriddenMethod, method);
            }
        }

        public void AddMethod(IMethodSymbol method)
        {
            AddOverrides(method);
            MethodLocator.Add(method);
            FunctionIndexer.Add(method);
        }

        public void AddMethod(IMethodSymbol method, MethodAnalysisData analysisData, IOperation operation)
        {
            AddOverrides(method);
            MethodLocator.Add(method);
            FunctionIndexer.Add(method,analysisData);
            if(!method.IsAbstract && operation!=null)
            {
                var walker = new MethodWalker(this, method);
                operation.Accept(walker);
            }
            
        }
    }
}