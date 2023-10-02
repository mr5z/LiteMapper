



using LiteMapper;
using System.Reflection;
using Test.Models.Domain;
using Test.Models.DTO;

var mappingService = new MappingService();

mappingService
    .Configure(Assembly.GetExecutingAssembly(), $"{nameof(Test)}.{nameof(Test.Models)}.{nameof(Test.Models.Domain)}", $"{nameof(Test)}.{nameof(Test.Models)}.{nameof(Test.Models.DTO)}")
    .On<User, UserDto>(user => user.LastName, a => a + "-lastname")
    .On<UserDto, User>(user => user.FirstName, new FirstNameMask());

var user = new User {  FirstName = "FirstName", LastName = "LastName" };

var dto = mappingService.Map<UserDto>(user);

var newUser = mappingService.Map<User>(dto);

Console.WriteLine($"domain: {newUser.FirstName}, {newUser.LastName}");
Console.WriteLine($"dto: {dto.FirstName}, {dto.LastName}");

class FirstNameMask : IDataTransformer<string>
{
    public string Transform(object data)
    {
        return data + "-transformed";
    }
}






