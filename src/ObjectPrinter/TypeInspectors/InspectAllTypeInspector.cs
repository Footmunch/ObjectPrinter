using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectPrinter.TypeInspectors
{
	public class InspectAllTypeInspector : ITypeInspector
	{
		/// <summary>If ShouldInspectType returns true, this inspector will handle the type</summary>
		public Func<object, Type, bool> ShouldInspectType { get; set; }

		/// <summary>If ShouldEvaluateMember returns true, the value will be evaluated for the member</summary>
		public Func<object, MemberInfo, bool> ShouldEvaluateMember { get; set; }

		/// <summary>If ShouldIncludeMember returns true (after member is evaluated), the member will be printed out</summary>
		public Func<object, ObjectInfo, bool> ShouldIncludeMember { get; set; }

        public static bool DefaultEnableCaching = true;
		public static BindingFlags DefaultMemberBindingFlags = BindingFlags.Instance
		                                                         | BindingFlags.Public
		                                                         | BindingFlags.GetProperty
		                                                         | BindingFlags.GetField;

		public static bool DefaultIncludeMethods = false;
		public static bool DefaultIncludeToStringWhenOverridden = true;


	    private IMemberCache _memberCache;
	    private BindingFlags _memberBindingFlags;

        public bool EnableCaching { get; set; }
	    public BindingFlags MemberBindingFlags
	    {
	        get { return _memberBindingFlags; }
	        set
	        {
	            _memberBindingFlags = value;
                _memberCache = EnableCaching 
                    ? (IMemberCache) new MemberCache(value) 
                    : new BigAlMemberCache(value);
	        }
	    }

	    public bool IncludeMethods { get; set; }
		public bool IncludeToStringWhenOverridden { get; set; }

		public InspectAllTypeInspector()
		{
		    EnableCaching = DefaultEnableCaching;
			MemberBindingFlags = DefaultMemberBindingFlags;
			IncludeMethods = DefaultIncludeMethods;
			IncludeToStringWhenOverridden = DefaultIncludeToStringWhenOverridden;
		}

		public virtual bool ShouldInspect(object objectToInspect, Type typeOfObjectToInspect)
		{
			return ShouldInspectType == null || ShouldInspectType(objectToInspect, typeOfObjectToInspect);
		}

        /// <summary>If ShouldEvaluate returns true, the value will be evaluated for the member</summary>
		protected virtual bool ShouldEvaluate(object instance, MemberInfo member)
		{
			return ShouldEvaluateMember == null || ShouldEvaluateMember(instance, member);
		}

        /// <summary>If ShouldInclude returns true (after member is evaluated), the member will be printed out</summary>
		protected virtual bool ShouldInclude(object instance, ObjectInfo info)
		{
			return ShouldIncludeMember == null || ShouldIncludeMember(instance, info);
		}

		public virtual IEnumerable<ObjectInfo> GetMemberList(object objectToInspect, Type typeOfObjectToInspect)
		{
			var type = objectToInspect.GetType();

		    var cache = _memberCache;

			var members = new List<ObjectInfo>();

			if (IncludeToStringWhenOverridden)
			{
				var toString = objectToInspect.ToString();
				if (string.CompareOrdinal(toString, objectToInspect.GetType().ToString()) != 0)
				{
					members.Add(new ObjectInfo {Name = "ToString()", Value = toString, Inspector = null});
				}
			}

		    members.AddRange(
		        cache.GetProperties(type)
		                    .Where(m => ShouldEvaluate(objectToInspect, m))
		                    .Select(p => new ObjectInfo
		                        {
		                            Name = p.Name,
		                            Value = ParsePropertyInfo(objectToInspect, p),
		                            Inspector = null
		                        })
		                    .Where(o => ShouldInclude(objectToInspect, o))
		        );

		    members.AddRange(
                cache.GetFields(type)
		                    .Where(m => ShouldEvaluate(objectToInspect, m))
		                    .Select(f => new ObjectInfo
		                        {
		                            Name = f.Name,
		                            Value = ParseFieldInfo(objectToInspect, f),
		                            Inspector = null
		                        })
		                    .Where(o => ShouldInclude(objectToInspect, o))
		        );

			if (IncludeMethods)
			{
			    members.AddRange(
                    cache.GetMethods(type)
			                    .Where(m => ShouldEvaluate(objectToInspect, m))
			                    .Select(m => new ObjectInfo
			                        {
			                            Name = m.Name,
			                            Value = ParseMethodInfo(objectToInspect, m),
			                            Inspector = null
			                        })
			                    .Where(o => ShouldInclude(objectToInspect, o))
			        );
			}

			return members;
		}

		protected virtual object ParseMethodInfo(object objectToInspect, MethodInfo methodInfo)
		{
			return methodInfo.ToString();
		}

		protected virtual object ParseFieldInfo(object objectToInspect, FieldInfo fieldInfo)
		{
			return ParseObjectValue(() => fieldInfo.GetValue(objectToInspect));
		}

		protected virtual object ParsePropertyInfo(object instance, PropertyInfo property)
		{
			return ParseObjectValue(() => property.GetValue(instance, null));
		}

		private static object ParseObjectValue(Func<object> func)
		{
			object value;
			try
			{
				value = func();
			}
			catch (TargetInvocationException tie)
			{
				value = tie.InnerException is NotImplementedException
				        	? "{not implemented}"
				        	: "{not available}";
			}
			catch (NotImplementedException /* nie */)
			{
				value = "{not implemented}";
			}
			catch (Exception e)
			{
				value = "{encountered error: " + e.GetType().Name + "}";
				//throw new InvalidOperationException(instance.GetType() + " - " + property + Environment.NewLine, e);
			}
			return value;
		}

	}
}