// 포션 생성 효과로 만들어진 오브젝트가 던진 사람·착탄 정보를 받는다
public interface IPotionSpawned
{
    public void OnSpawnedByPotion(in PotionImpactContext context);
}
