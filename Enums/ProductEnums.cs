namespace api.Enums
{
    public class ProductEnums
    {
        public enum ProductCategory
        {
            Card,
            Sealed,
            Accessory,
            Bundle
        }

        public enum CardRarity
        {
            Common,
            Uncommon,
            Rare,
            Mythic
        }

        public enum CardCondition
        {
            Mint,
            NearMint,
            LightlyPlayed,
            ModeratelyPlayed,
            HeavilyPlayed,
            Damaged
        }

        public enum CardType
        {
            Creature,
            Instant,
            Sorcery,
            Artifact,
            Enchantment,
            Planeswalker,
            Land
        }
    }
}
