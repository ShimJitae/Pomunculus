using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    [CreateAssetMenu(fileName = "Potion", menuName = "Pomunculus/Items/Potion")]
    public class PotionData : ScriptableObject
    {
        [Header("기본 정보")]
        public int Id;
        public string ItemName;

        [Header("기본 설명")]
        [TextArea(1, 5)]
        public string Description;

        [Header("세부 설명")]
        [TextArea(1, 5)]
        public string Details;

        [Header("아이콘")]
        public Sprite Icon;

        [Header("원소 수치")]
        public float PhysicsElement;
        public float FireElement;
        public float ColdElement;
        public float WindElement;

        [Header("픽업 프리팹")]
        [SerializeField] private PotionPickup pickupPrefab;
        public PotionPickup PickupPrefab => pickupPrefab;

        [Header("던지기")]
        [Tooltip("던질 때 생성할 프리팹. 비우면 빈 병")]
        [SerializeField] private ThrownProjectile projectilePrefab;

        [Tooltip("착탄 시 대상에게 순서대로 적용할 효과")]
        [SerializeReference] private List<PotionEffect> effects = new List<PotionEffect>();

        public ThrownProjectile ProjectilePrefab => projectilePrefab;
        public IReadOnlyList<PotionEffect> Effects => effects;

        // CSV에서 사용하는 상태별 이름. 퍼센트가 없는 상태는 PercentToken을 비워 둔다.
        private static readonly Dictionary<StatusEffectType, (string Prefix, string PercentToken)> StatusTokens =
            new Dictionary<StatusEffectType, (string Prefix, string PercentToken)>
            {
                { StatusEffectType.Burn,         ("burn",       null) },
                { StatusEffectType.Haste,        ("haste",      "{hasteSpeed}") },
                { StatusEffectType.Stun,         ("stun",       null) },
                { StatusEffectType.Vulnerable,   ("vulnerable", "{vulnerablePercent}") },
                { StatusEffectType.Slime,        ("slime",      "{slimeSlow}") },
                { StatusEffectType.Poison,       ("poison",     "{poisonReduction}") },
                { StatusEffectType.Wet,          ("wet",        "{wetSlow}") },
                { StatusEffectType.Chill,        ("chill",      "{chillSlow}") },
                { StatusEffectType.Petrify,      ("petrify",    null) },
                { StatusEffectType.Freeze,       ("freeze",     null) },
                { StatusEffectType.ResistHeat,   ("heat",       null) },
                { StatusEffectType.ResistCold,   ("cold",       null) },
                { StatusEffectType.ResistDisease, ("disease",   null) }
            };
        public string GetDetails()
        {
            if (string.IsNullOrWhiteSpace(Details))
                return Description;

            string text = Details;
            foreach (PotionEffect effect in Effects)
            {
                switch (effect)
                {
                    case DamagePotionEffect damage:
                        text = text.Replace("{damage}", damage.Amount.ToString());
                        break;

                    case HealPotionEffect heal:
                        text = text.Replace("{healAmount}", heal.Amount.ToString());
                        break;

                    case ShieldPotionEffect shield:
                        text = text
                            .Replace("{shieldAmount}", shield.Amount.ToString())
                            .Replace("{shieldDuration}", shield.Duration.ToString("0.##"));
                        break;

                    case ZonePotionEffect zone:
                        text = text.Replace("{zoneRadius}", zone.AreaRadius.ToString("0.##"));
                        foreach (PotionEffect tickEffect in zone.TickEffects)
                        {
                            if (tickEffect is DamagePotionEffect zoneDamage)
                            {
                                text = text.Replace("{zoneDamage}", zoneDamage.Amount.ToString());
                                break;
                            }
                        }
                        break;

                    case StatusPotionEffect status:
                        if (!StatusTokens.TryGetValue(status.StatusType, out var tokens))
                            break;

                        var manager = StatusEffectManager.Instance;
                        var data = manager != null ? manager.GetSOData(status.StatusType) : null;
                        if (data == null)
                            return Description;

                        text = text
                            .Replace("{" + tokens.Prefix + "Duration}", data.Duration.ToString("0.##"))
                            .Replace("{" + tokens.Prefix + "Stacks}", status.StackCount.ToString());

                        if (tokens.PercentToken != null)
                            text = text.Replace(tokens.PercentToken, (data.ValuePerStack * 100f).ToString("0.##"));

                        if (status.StatusType == StatusEffectType.Burn)
                        {
                            text = text
                                .Replace("{burnInterval}", data.TickSec.ToString("0.##"))
                                .Replace("{burnDamage}", data.TickDamagePerStack.ToString());
                        }
                        break;
                }
            }
            return text;
        }
    }
}