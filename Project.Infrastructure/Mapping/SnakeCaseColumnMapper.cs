using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;

namespace Project.Infrastructure.Mapping
{


    public sealed class SnakeCaseToPascalCaseTypeMapper : SqlMapper.ITypeMap
    {
        private readonly Type _type;
        private readonly SqlMapper.ITypeMap _fallbackMapper;

        public SnakeCaseToPascalCaseTypeMapper(Type type)
        {
            _type = type;
            _fallbackMapper = new DefaultTypeMap(type);
        }

        public System.Reflection.ConstructorInfo? FindConstructor(
            string[] names, Type[] types)
            => _fallbackMapper.FindConstructor(names, types);

        public System.Reflection.ConstructorInfo? FindExplicitConstructor()
            => _fallbackMapper.FindExplicitConstructor();

        public SqlMapper.IMemberMap? GetConstructorParameter(
            System.Reflection.ConstructorInfo constructor, string columnName)
            => _fallbackMapper.GetConstructorParameter(constructor, columnName);

        public SqlMapper.IMemberMap? GetMember(string columnName)
        {
            var pascalName = SnakeToPascal(columnName);
            return _fallbackMapper.GetMember(pascalName)
                ?? _fallbackMapper.GetMember(columnName);
        }

        private static string SnakeToPascal(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase)) return snakeCase;

            return Regex.Replace(snakeCase, @"(^|_)([a-z])",
                m => m.Groups[2].Value.ToUpperInvariant());
        }
    }


    public static class DapperTypeMapRegistrar
    {
        public static void Register(params Type[] types)
        {
            foreach (var type in types)
            {
                SqlMapper.SetTypeMap(type, new SnakeCaseToPascalCaseTypeMapper(type));
            }
        }
    }


}
