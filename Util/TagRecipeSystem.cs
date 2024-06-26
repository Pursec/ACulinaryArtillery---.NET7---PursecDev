using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System.Reflection.Metadata.Ecma335;
using Vintagestory.GameContent;

namespace ACulinaryArtillery.Util
{

    public class TagIngredient : IRecipeIngredient
    {
        public EnumItemClass Type;

        public int Quantity = 1;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject RecipeAttributes;

        public string[] AllowedTags;

        public string[] BannedTags;

        public string[] SpecialTags;

        public string Category;

        public bool IsTool;

        public int ToolDurabilityCost = 1;

        public string[] AllowedVariants;

        public string[] SkipVariants;

        public JsonItemStack ReturnedStack;

        public ItemStack ResolvedItemstack;

        public bool IsWildCard;

        public bool IsTag;

        public AssetLocation Code { get; set; }

        public string Name { get; set; }

        public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging)
        {
            if (ReturnedStack != null)
            {
                ReturnedStack.Resolve(resolver, sourceForErrorLogging + " recipe with output ", Code);
            }
            if (AllowedTags != null || BannedTags != null || Category != null || SpecialTags != null)
            {
                IsTag = true;
                return true;
            }
            if (Code.Path.Contains('*'))
            {
                IsWildCard = true;
                return true;
            }

            if (Type == EnumItemClass.Block)
            {
                Block block = resolver.GetBlock(Code);
                if (block == null || block.IsMissing)
                {
                    resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                    return false;
                }

                ResolvedItemstack = new ItemStack(block, Quantity);
            }
            else
            {
                Item item = resolver.GetItem(Code);
                if (item == null || item.IsMissing)
                {
                    resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                    return false;
                }

                ResolvedItemstack = new ItemStack(item, Quantity);
            }

            if (Attributes != null)
            {
                IAttribute attribute = Attributes.ToAttribute();
                if (attribute is ITreeAttribute)
                {
                    ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
                }
            }

            return true;
        }

        public bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStacksize = true)
        {
            if (inputStack == null)
            {
                return false;
            }
            if (IsTag)
            {
                string[] inputTags = inputStack.Collectible.Attributes["Tags"].AsArray<string>();
                string category = inputStack.Collectible.Attributes["TagCategory"].AsString();
                if (SpecialTags != null && inputTags != null)
                {
                    if (SpecialTags.Intersect(inputTags).Any())
                    {
                        return true;
                    }
                }
                if (Category != null)
                {
                    if (category is null || Category != category)
                    {
                        return false;
                    }
                    if (BannedTags == null && AllowedTags == null)
                    {
                        return true;
                    }
                }
                if (inputTags != null)
                {
                    if (BannedTags != null && BannedTags.Intersect(inputTags).Any())
                    {
                        return false;
                    }
                    if (AllowedTags != null && !AllowedTags.Intersect(inputTags).Any())
                    {
                        return false;
                    }
                }
                else { return false; }
                return true;


            }
            if (IsWildCard)
            {
                if (Type != inputStack.Class)
                {
                    return false;
                }

                if (!WildcardUtil.Match(Code, inputStack.Collectible.Code, AllowedVariants))
                {
                    return false;
                }

                if (SkipVariants != null && WildcardUtil.Match(Code, inputStack.Collectible.Code, SkipVariants))
                {
                    return false;
                }

                if (checkStacksize && inputStack.StackSize < Quantity)
                {
                    return false;
                }
            }
            else
            {
                if (!ResolvedItemstack.Satisfies(inputStack))
                {
                    return false;
                }

                if (checkStacksize && inputStack.StackSize < ResolvedItemstack.StackSize)
                {
                    return false;
                }
            }

            return true;
        }

        public TagIngredient Clone()
        {
            return CloneTo<TagIngredient>();
        }
        public T CloneFrom<T>(CraftingRecipeIngredient craftingRecipeIngredient) where T : TagIngredient, new()
        {
            T val = new T
            {
                Code = craftingRecipeIngredient.Code?.Clone(),
                Type = craftingRecipeIngredient.Type,
                Name = craftingRecipeIngredient.Name,
                Quantity = craftingRecipeIngredient.Quantity,
                IsWildCard = craftingRecipeIngredient.IsWildCard,
                IsTag = false,
                BannedTags = null,
                AllowedTags = null,
                SpecialTags = null,
                Category = null,
                IsTool = craftingRecipeIngredient.IsTool,
                ToolDurabilityCost = craftingRecipeIngredient.ToolDurabilityCost,
                AllowedVariants = ((craftingRecipeIngredient.AllowedVariants == null) ? null : ((string[])craftingRecipeIngredient.AllowedVariants.Clone())),
                SkipVariants = ((craftingRecipeIngredient.SkipVariants == null) ? null : ((string[])craftingRecipeIngredient.SkipVariants.Clone())),
                ResolvedItemstack = craftingRecipeIngredient.ResolvedItemstack?.Clone(),
                ReturnedStack = craftingRecipeIngredient.ReturnedStack?.Clone(),
                RecipeAttributes = craftingRecipeIngredient.RecipeAttributes?.Clone()
            };
            if (craftingRecipeIngredient.Attributes != null)
            {
                val.Attributes = craftingRecipeIngredient.Attributes.Clone();
            }

            return val;
        }
        public T CloneTo<T>() where T : TagIngredient, new()
        {
            T val = new T
            {
                Code = Code?.Clone(),
                Type = Type,
                Name = Name,
                Quantity = Quantity,
                IsWildCard = IsWildCard,
                IsTag = (IsTag == null) ? false : IsTag,
                BannedTags = ((BannedTags == null) ? null : ((string[])BannedTags.Clone())),
                AllowedTags = ((AllowedTags == null) ? null : ((string[])AllowedTags.Clone())),
                SpecialTags = ((SpecialTags == null) ? null : ((string[])SpecialTags.Clone())),
                Category = Category == null ? null : Category,
                IsTool = IsTool,
                ToolDurabilityCost = ToolDurabilityCost,
                AllowedVariants = ((AllowedVariants == null) ? null : ((string[])AllowedVariants.Clone())),
                SkipVariants = ((SkipVariants == null) ? null : ((string[])SkipVariants.Clone())),
                ResolvedItemstack = ResolvedItemstack?.Clone(),
                ReturnedStack = ReturnedStack?.Clone(),
                RecipeAttributes = RecipeAttributes?.Clone()
            };
            if (Attributes != null)
            {
                val.Attributes = Attributes.Clone();
            }

            return val;
        }

        public override string ToString()
        {
            return Type.ToString() + " code " + Code ?? "TagIngredient";
        }

        public void FillPlaceHolder(string key, string value)
        {
            if (Code != null)
            {
                Code = Code.CopyWithPath(Code.Path.Replace("{" + key + "}", value));
            }
            Attributes?.FillPlaceHolder(key, value);
            RecipeAttributes?.FillPlaceHolder(key, value);
        }

        public virtual void ToBytes(BinaryWriter writer)
        {
            writer.Write(IsTag);
            writer.Write(SpecialTags != null);
            if (SpecialTags != null)
            {
                writer.Write((int)SpecialTags.Length);
                foreach (var Tag in SpecialTags)
                {
                    writer.Write(Tag);
                }
            }
            writer.Write(Category != null);
            if (Category != null) { writer.Write(Category); }
            writer.Write(AllowedTags != null);
            if (AllowedTags != null)
            {
                writer.Write((int)AllowedTags.Length);
                foreach (var Tag in AllowedTags)
                {
                    writer.Write(Tag);
                }
            }
            writer.Write(BannedTags != null);
            if (BannedTags != null)
            {
                writer.Write(BannedTags.Length);
                foreach (var Tag in BannedTags)
                {
                    writer.Write(Tag);
                }
            }
            writer.Write(IsWildCard);
            writer.Write((int)Type);
            writer.Write(Code != null);
            if (Code != null) { writer.Write(Code.ToShortString()); }
            writer.Write(Quantity);
            if (!IsWildCard)
            {
                writer.Write(ResolvedItemstack != null);
                ResolvedItemstack?.ToBytes(writer);
            }

            writer.Write(IsTool);
            writer.Write(ToolDurabilityCost);
            writer.Write(AllowedVariants != null);
            if (AllowedVariants != null)
            {
                writer.Write(AllowedVariants.Length);
                for (int i = 0; i < AllowedVariants.Length; i++)
                {
                    writer.Write(AllowedVariants[i]);
                }
            }

            writer.Write(SkipVariants != null);
            if (SkipVariants != null)
            {
                writer.Write(SkipVariants.Length);
                for (int j = 0; j < SkipVariants.Length; j++)
                {
                    writer.Write(SkipVariants[j]);
                }
            }

            writer.Write(ReturnedStack?.ResolvedItemstack != null);
            if (ReturnedStack?.ResolvedItemstack != null)
            {
                ReturnedStack.ToBytes(writer);
            }

            if (RecipeAttributes != null)
            {
                writer.Write(value: true);
                writer.Write(RecipeAttributes.ToString());
            }
            else
            {
                writer.Write(value: false);
            }
        }

        public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            IsTag = reader.ReadBoolean();
            if (reader.ReadBoolean())
            {
                SpecialTags = new string[reader.ReadInt32()];
                for (int i = 0; (i < SpecialTags.Length); i++)
                {
                    SpecialTags[i] = reader.ReadString();
                }
            }
            if (reader.ReadBoolean())
            {
                Category = reader.ReadString();
            }
            if (reader.ReadBoolean())
            {
                AllowedTags = new string[reader.ReadInt32()];
                for (int i = 0; i < AllowedTags.Length; i++)
                {
                    AllowedTags[i] = reader.ReadString();
                }
            }
            if (reader.ReadBoolean())
            {
                BannedTags = new string[reader.ReadInt32()];
                for (int i = 0; i < BannedTags.Length; i++)
                {
                    BannedTags[i] = reader.ReadString();
                }
            }
            IsWildCard = reader.ReadBoolean();
            Type = (EnumItemClass)reader.ReadInt32();
            if (reader.ReadBoolean()) { Code = new AssetLocation(reader.ReadString()); }

            Quantity = reader.ReadInt32();
            if (!IsWildCard && reader.ReadBoolean())
            {
                ResolvedItemstack = new ItemStack(reader, resolver);
            }

            IsTool = reader.ReadBoolean();
            ToolDurabilityCost = reader.ReadInt32();
            if (reader.ReadBoolean())
            {
                AllowedVariants = new string[reader.ReadInt32()];
                for (int i = 0; i < AllowedVariants.Length; i++)
                {
                    AllowedVariants[i] = reader.ReadString();
                }
            }

            if (reader.ReadBoolean())
            {
                SkipVariants = new string[reader.ReadInt32()];
                for (int j = 0; j < SkipVariants.Length; j++)
                {
                    SkipVariants[j] = reader.ReadString();
                }
            }

            if (reader.ReadBoolean())
            {
                ReturnedStack = new JsonItemStack();
                ReturnedStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnedStack.ResolvedItemstack.ResolveBlockOrItem(resolver);
            }

            if (reader.ReadBoolean())
            {
                RecipeAttributes = new JsonObject(JToken.Parse(reader.ReadString()));
            }
        }

        public static explicit operator TagIngredient(CraftingRecipeIngredient v)
        {
            if(v == null) return null;
            TagIngredient tag = new TagIngredient();
            return tag.CloneFrom<TagIngredient>(v);
        }
    }
    public class TagDoughIngredient
    {
        public TagIngredient[] Inputs;

        public TagIngredient GetMatch(ItemStack stack)
        {
            if (stack == null) return null;
            ACulinaryArtillery.logger.Debug("Trying to match: " + stack.ToString());
            for (int i = 0; i < Inputs.Length; i++)
            {
                if (Inputs[i].SatisfiesAsIngredient(stack)) return Inputs[i];
            }

            return null;
        }

        public bool Resolve(IWorldAccessor world, string debug)
        {
            bool ok = true;

            for (int i = 0; i < Inputs.Length; i++)
            {
                ok &= Inputs[i].Resolve(world, debug);
            }

            return ok;
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Inputs = new TagIngredient[reader.ReadInt32()];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new TagIngredient();
                Inputs[i].FromBytes(reader, resolver);
                Inputs[i].Resolve(resolver, "Dough Ingredient (FromBytes)");
            }
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Inputs.Length);
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].ToBytes(writer);
            }

        }

        public TagDoughIngredient Clone()
        {
            TagIngredient[] newings = new TagIngredient[Inputs.Length];

            for (int i = 0; i < Inputs.Length; i++)
            {
                newings[i] = Inputs[i].Clone();
            }

            return new TagDoughIngredient()
            {
                Inputs = newings
            };
        }
    }
    public class TagDoughRecipe : IByteSerializable
    {
        public string Code = "something";
        public AssetLocation Name { get; set; }
        public bool Enabled { get; set; } = true;


        public TagDoughIngredient[] Ingredients;

        public JsonItemStack Output;

        public ItemStack TryCraftNow(ICoreAPI api, ItemSlot[] inputslots)
        {
            var matched = pairInput(inputslots);

            ItemStack mixedStack = Output.ResolvedItemstack.Clone();
            mixedStack.StackSize = getOutputSize(matched);

            if (mixedStack.StackSize <= 0) return null;

            /*
            TransitionableProperties[] props = mixedStack.Collectible.GetTransitionableProperties(api.World, mixedStack, null);
            TransitionableProperties perishProps = props != null && props.Length > 0 ? props[0] : null;

            if (perishProps != null)
            {
                CollectibleObject.CarryOverFreshness(api, inputslots, new ItemStack[] { mixedStack }, perishProps);
            }*/

            IExpandedFood food;
            if ((food = mixedStack.Collectible as IExpandedFood) != null) food.OnCreatedByKneading(matched, mixedStack);

            foreach (var val in matched)
            {
                val.Key.TakeOut(((TagIngredient)(val.Value)).Quantity * (mixedStack.StackSize / Output.StackSize));
                val.Key.MarkDirty();
            }

            return mixedStack;
        }

        public bool Matches(IWorldAccessor worldForResolve, ItemSlot[] inputSlots)
        {
            int outputStackSize = 0;

            List<KeyValuePair<ItemSlot, TagIngredient>> matched = pairInput(inputSlots);
            if (matched == null) return false;

            outputStackSize = getOutputSize(matched);

            return outputStackSize >= 0;
        }

        List<KeyValuePair<ItemSlot, TagIngredient>> pairInput(ItemSlot[] inputStacks)
        {
            List<int> alreadyFound = new List<int>();

            Queue<ItemSlot> inputSlotsList = new Queue<ItemSlot>();
            foreach (var val in inputStacks) if (!val.Empty) inputSlotsList.Enqueue(val);

            if (inputSlotsList.Count != Ingredients.Length) return null;

            List<KeyValuePair<ItemSlot,TagIngredient>> matched = new List<KeyValuePair<ItemSlot, TagIngredient>>();

            while (inputSlotsList.Count > 0)
            {
                ItemSlot inputSlot = inputSlotsList.Dequeue();
                bool found = false;

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    TagIngredient ingred = Ingredients[i].GetMatch(inputSlot.Itemstack);

                    if (ingred != null && !alreadyFound.Contains(i))
                    {
                        matched.Add(new KeyValuePair<ItemSlot, TagIngredient>(inputSlot, ingred));
                        alreadyFound.Add(i);
                        found = true;
                        break;
                    }
                }

                if (!found) return null;
            }

            // We're missing ingredients
            if (matched.Count != Ingredients.Length)
            {
                return null;
            }

            return matched;
        }


        int getOutputSize(List<KeyValuePair<ItemSlot, TagIngredient>> matched)
        {
            int outQuantityMul = -1;

            foreach (var val in matched)
            {
                ItemSlot inputSlot = val.Key;
                TagIngredient ingred = (TagIngredient)val.Value;
                int posChange = inputSlot.StackSize / ingred.Quantity;

                if (posChange < outQuantityMul || outQuantityMul == -1) outQuantityMul = posChange;
            }

            if (outQuantityMul == -1)
            {
                return -1;
            }


            foreach (var val in matched)
            {
                ItemSlot inputSlot = val.Key;
                TagIngredient ingred = (TagIngredient)val.Value;


                // Must have same or more than the total crafted amount
                if (inputSlot.StackSize < ingred.Quantity * outQuantityMul) return -1;

            }

            outQuantityMul = 1;
            return Output.StackSize * outQuantityMul;
        }

        public string GetOutputName()
        {
            return Lang.Get("aculinaryartillery:Will make {0}", Output.ResolvedItemstack.GetName());
        }

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            bool ok = true;

            for (int i = 0; i < Ingredients.Length; i++)
            {
                ok &= Ingredients[i].Resolve(world, sourceForErrorLogging);
            }

            ok &= Output.Resolve(world, sourceForErrorLogging);


            return ok;
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Code);
            writer.Write(Ingredients.Length);
            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i].ToBytes(writer);
            }

            Output.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadString();
            Ingredients = new TagDoughIngredient[reader.ReadInt32()];

            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i] = new TagDoughIngredient();
                Ingredients[i].FromBytes(reader, resolver);
                Ingredients[i].Resolve(resolver, "Dough Recipe (FromBytes)");
            }

            Output = new JsonItemStack();
            Output.FromBytes(reader, resolver.ClassRegistry);
            Output.Resolve(resolver, "Dough Recipe (FromBytes)");
        }

        public TagDoughRecipe Clone()
        {
            TagDoughIngredient[] ingredients = new TagDoughIngredient[Ingredients.Length];
            for (int i = 0; i < Ingredients.Length; i++)
            {
                ingredients[i] = Ingredients[i].Clone();
            }

            return new TagDoughRecipe()
            {
                Output = Output.Clone(),
                Code = Code,
                Enabled = Enabled,
                Name = Name,
                Ingredients = ingredients
            };
        }

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

            if (Ingredients == null || Ingredients.Length == 0) return mappings;

            foreach (var ingreds in Ingredients)
            {
                if (ingreds.Inputs.Length <= 0) continue;
                TagIngredient ingred = ingreds.Inputs[0];
                if (ingred?.AllowedTags != null || ingred?.Category != null || ingred?.BannedTags != null || ingred?.SpecialTags != null) continue;
                if (ingred == null || !ingred.Code.Path.Contains("*") || ingred.Name == null) continue;

                int wildcardStartLen = ingred.Code.Path.IndexOf("*");
                int wildcardEndLen = ingred.Code.Path.Length - wildcardStartLen - 1;

                List<string> codes = new List<string>();

                if (ingred.Type == EnumItemClass.Block)
                {
                    for (int i = 0; i < world.Blocks.Count; i++)
                    {
                        if (world.Blocks[i].Code == null || world.Blocks[i].IsMissing) continue;

                        if (WildcardUtil.Match(ingred.Code, world.Blocks[i].Code))
                        {
                            string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
                            string codepart = code.Substring(0, code.Length - wildcardEndLen);
                            if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                            codes.Add(codepart);

                        }
                    }
                }
                else
                {
                    for (int i = 0; i < world.Items.Count; i++)
                    {
                        if (world.Items[i].Code == null || world.Items[i].IsMissing) continue;

                        if (WildcardUtil.Match(ingred.Code, world.Items[i].Code))
                        {
                            string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
                            string codepart = code.Substring(0, code.Length - wildcardEndLen);
                            if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                            codes.Add(codepart);
                        }
                    }
                }

                mappings[ingred.Name] = codes.ToArray();
            }

            return mappings;
        }
    }
}
