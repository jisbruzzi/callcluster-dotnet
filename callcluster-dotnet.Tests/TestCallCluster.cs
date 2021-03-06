using System;
using Xunit;
using callcluster_dotnet;
using callcluster_dotnet.dto;
using System.Linq;

namespace callcluster_dotnet.Tests
{
    [Collection("Sequential")]
    public class TestCallCluster
    {
        [Fact]
        public async void CallsToGenericMethods()
        {
            CallgraphDTO dto = await Utils.Extract("../callcluster-dotnet.sln");
            CommunityDTO project = dto.community.Namespace("callcluster_dotnet");
            
            long? addClassMethod = project
            ?.Community("CallgraphCollector")
            ?.Function("AddClass(INamedTypeSymbol)",dto);

            long? addTreeMethod = project
            ?.Community("Tree")
            ?.Function("Add(T, T)",dto);

            CallgraphAssert.CallPresent(dto,addClassMethod.Value,addTreeMethod.Value);
        }

        [Fact]
        public async void CallsToAnotherGenericMethod()
        {
            CallgraphDTO dto = await Utils.Extract("../callcluster-dotnet.sln");
            CommunityDTO project = dto.community.Namespace("callcluster_dotnet");
            
            long? getCallDTOs = project
            ?.Community("CallgraphCollector")
            ?.Function("GetCallDTOs()",dto);

            long? descendantsOf = project
            ?.Community("Tree")
            ?.Function("DescendantsOf(T)",dto);

            CallgraphAssert.CallPresent(dto,getCallDTOs.Value,descendantsOf.Value);
        }

        [Fact]
        public async void CallsFromTestingToExtractor()
        {
            CallgraphDTO dto = await Utils.Extract("../callcluster-dotnet.sln");
            CommunityDTO extractor = dto.community.Community("callcluster-dotnet");
            CommunityDTO tests = dto.community.Community("callcluster-dotnet.Tests");
            
            long? realExtract = extractor
            .Namespace("callcluster_dotnet")
            ?.Community("Program")
            ?.Function("Extract(String)",dto);

            long? testingExtract = tests
            .Namespace("callcluster_dotnet")
            ?.Community("Tests")
            ?.Community("Utils")
            ?.Function("Extract(String)",dto);

            var extracts = dto.functions.Where(f=>f.name=="Extract(String)").ToList();
            Assert.Equal(2,extracts.Count());

            CallgraphAssert.CallPresent(dto,testingExtract.Value,realExtract.Value);
        }

        [Fact]
        public async void CallsToInterfaceAreResolved()
        {
            CallgraphDTO dto = await Utils.Extract("../callcluster-dotnet.sln");
            CommunityDTO project = dto.community
            .Community("callcluster-dotnet")
            .Namespace("callcluster_dotnet");
            
            long? visitClassDeclaration = project
            ?.Community("CSharpCallgraphWalker")
            ?.Function("VisitClassDeclaration(ClassDeclarationSyntax)",dto);

            long? called = project
            ?.Community("CallgraphCollector")
            ?.Function("AddClass(INamedTypeSymbol)",dto);

            CallgraphAssert.CallPresent(
                dto,
                visitClassDeclaration.Value,
                called.Value
            );

            var addClassInstances = dto.functions
            .Where(f=>f.name=="AddClass(INamedTypeSymbol)" && f.written)
            .ToList();
            Assert.Equal(1,addClassInstances.Count());
        }
    }
}
