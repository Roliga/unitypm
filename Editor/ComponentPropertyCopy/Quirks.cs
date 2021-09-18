using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityUtils;

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
				["Bone Mappings"] = (inComp, outComp) =>
				{
					SkinnedMeshRenderer inMesh = (SkinnedMeshRenderer)inComp;
					SkinnedMeshRenderer outMesh = (SkinnedMeshRenderer)outComp;

					Transform FindTransform (Transform transform)
                    {
						foreach (Transform it in inMesh.bones)
							if (transform.name == it.name)
								return it;
						return null;
					}

					int count = 0;
					List<Transform> bones = new List<Transform>();
					string switches = "", kept = "";

					foreach (Transform ob in outMesh.bones)
                    {
						Transform ib = FindTransform(ob);

						if (ib == null)
						{
							bones.Add(ob);
							kept += $"{ScriptUtils.GetTransformPath(ob)}\n";
						}
						else
						{
							bones.Add(ib);
							switches += $"{ScriptUtils.GetTransformPath(ob)} --> {ScriptUtils.GetTransformPath(ib)}\n";
							count++;
                        }
					}

					outMesh.bones = bones.ToArray();
					Debug.Log($"Switched {count} out of {outMesh.bones.Length} bones on {outMesh.name}\n" +
						$"\n" +
						$"Switched:\n" +
						switches +
						$"\n" +
						$"Kept:\n" +
						kept);
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
