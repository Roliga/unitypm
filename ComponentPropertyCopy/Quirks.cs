using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ComponentPropertyCopy
{
	public class QuirkList : Dictionary<string, Action<Component, Component>> { }

	public partial class ComponentPropertyCopy
	{
		public static Dictionary<Type, QuirkList> Quirks = new Dictionary<Type, QuirkList>()
		{
			[typeof(SkinnedMeshRenderer)] = new QuirkList()
			{
				["Blend Shapes"] = (inComp, outComp) =>
				{
					SkinnedMeshRenderer inMesh = (SkinnedMeshRenderer)inComp;
					SkinnedMeshRenderer outMesh = (SkinnedMeshRenderer)outComp;
					for (int i = 0; i < inMesh.sharedMesh.blendShapeCount; i++)
					{
						string name = inMesh.sharedMesh.GetBlendShapeName(i);
						int toIndex = outMesh.sharedMesh.GetBlendShapeIndex(name);

						if (toIndex != -1)
							outMesh.SetBlendShapeWeight(toIndex, inMesh.GetBlendShapeWeight(i));
					}
				},
				["Bones"] = (inComp, outComp) =>
				{
					SkinnedMeshRenderer inMesh = (SkinnedMeshRenderer)inComp;
					SkinnedMeshRenderer outMesh = (SkinnedMeshRenderer)outComp;
				}
			},
			[typeof(MeshCollider)] = new QuirkList()
			{
				["What"] = (inComp, outComp) => { },
				["The"] = (inComp, outComp) => { }
			}
		};
	}
}
