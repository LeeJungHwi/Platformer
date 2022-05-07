using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    // 사용자정의 컴포넌트
    public float maxSpeed; 
    public float jumpPower; 
    int cnt = 2;

    // 유니티엔진 컴포넌트
    Rigidbody2D rigid; 
    SpriteRenderer spriteRenderer; 
    CapsuleCollider2D capsuleCollider;
    Animator anim;

    void Awake() // 초기화
    {
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        rigid = GetComponent<Rigidbody2D>();               
        spriteRenderer = GetComponent<SpriteRenderer>();    
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 점프(2단)
        if (Input.GetButtonDown("Jump") && cnt > 0) // 점프키를 누르면서 점프횟수가 0보다 크다면
        {
            // 점프할때마다 Y축 속도를 0으로 초기화
            rigid.velocity = new Vector2(rigid.velocity.x, 0);

            // 위쪽으로 힘을 준다
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);

            // 에니메이션
            anim.SetBool("isJump", true);

            // 점프 카운팅 감소
            cnt--;
        }

        // 점프 할때 이동속도 제한
        if (Input.GetButtonUp("Horizontal")) // GetButtonUp() -> 버튼을 눌렀다가 땠을경우 True 반환
        {
            rigid.velocity = new Vector2(rigid.velocity.normalized.x * 0.5f, rigid.velocity.y); // normalized -> 벡터 크기를 1로만든 상태
        }

        // 방향 전환
        if (Input.GetButton("Horizontal")) // GetButtonDown() -> 버튼을 한번 누를때 True 반환, 키입력이 겹치는 구간 발생(문워크), GetButton() -> 버튼을 누를때 항상 체크
            spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1; // 방향전환 X축

        // 에니메이션
        if (Mathf.Abs(rigid.velocity.x) < 0.3) // 행동을 멈추면, Mathf.Abs() -> 절대값
            anim.SetBool("isRun", false);
        else
            anim.SetBool("isRun", true);
    }

    void FixedUpdate()
    {
        // 이동 속도
        float h = Input.GetAxisRaw("Horizontal"); // GetAxisRaw() -> 방향키값을 축값으로 받아옵니다, -1(왼쪽) 1(오른쪽)
        rigid.AddForce(Vector2.right * h * 2, ForceMode2D.Impulse); // AddForce() -> 주어진 벡터 크기만큼 힘을 준다, ForceMode2D.Impulse -> 충격량을 rigidbody2D에 적용한다

        // 최대 이동 속도 제한
        if (rigid.velocity.x > maxSpeed) // Right Max Speed
            rigid.velocity = new Vector2(maxSpeed, rigid.velocity.y); // Vector2(float x, float y), velocity -> 직접 속도를 바꾼다
        else if (rigid.velocity.x < maxSpeed * (-1)) // Left Max Speed
            rigid.velocity = new Vector2(maxSpeed * (-1), rigid.velocity.y);

        // 착지 상태
        if (rigid.velocity.y < 0) // y축의 속도가 내려가는 상태이면
        {
            // DrawRay(시작, 방향, 색) -> 에디터상에서만 빛을 쏜다
            Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0));

            // RaycastHit2D -> 빛에 닿은 객체, Raycast(시작, 방향, 길이, 레이어마스크) -> 빛을 쏴서 객체를 검색한다, LayerMask -> 특정 레이어를 필터링해서 검색한다
            RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1, LayerMask.GetMask("Platform"));

            if (rayHit.collider != null) // 빛을 맞은 객체의 정보가 있다면
            {
                if (rayHit.distance < 0.5f) // 빛을 닿은 거리가 0.5보다 작으면
                {
                    anim.SetBool("isJump", false); // 점프하는 상태가 아니다
                    cnt = 2; // 착지 상태 이므로, 다시 2단 점프 할 수 있게 점프 카운팅 2로 초기화
                }
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy") // Enemy와 충돌하면
        {
            OnDamaged(collision.transform.position); // 캐릭터가 데미지를 입는다
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Item") // Item과 충돌하면
        {
            // 코인의 종류에 따라서 점수 증가
            bool isCoin = collision.gameObject.name.Contains("Coin"); // Contains(비교문) -> 대상 문자열에 비교문이 있으면 true 반환
            bool isStar = collision.gameObject.name.Contains("Star"); // Contains(비교문) -> 대상 문자열에 비교문이 있으면 true 반환

            // if (isCoin)
            //     gameManager.stagePoint += 100;
            // else if (isStar)
            //     gameManager.stagePoint += 1000;

            // 아이템이 사라짐
            collision.gameObject.SetActive(false);
        }
        else if (collision.gameObject.tag == "Finish") // Finish와 충돌 했을때 1. 다음스테이지로
        {
            // 다음 스테이지
            // gameManager.NextStage();
        }
    }

    void OnDamaged(Vector2 targetPos) // 캐릭터가 맞았을때 사용하는 함수
    {
        // 레이어를 11.CharacterDamaged로 변경 -> Enemy 태그의 오브젝트와 닿아도 HP가 깎이지 않게 프로젝트 세팅
        gameObject.layer = 11;

        // 잠시 투명
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        // 캐릭터 위치 - Enemy 위치 > 0 일때는 캐릭터가 Enemy보다 오른쪽에 있으므로, 1(오른쪽) 으로 튕겨나간다
        int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
        rigid.AddForce(new Vector2(dirc, 1) * 7, ForceMode2D.Impulse);

        // 에니메이션
        anim.SetTrigger("doDamaged");

        // 무적시간
        Invoke("OffDamaged", 3);
    }

    void OffDamaged() // 무적 해제 함수
    {
        // 레이어를 10.Character로 변경
        gameObject.layer = 10;

        // 투명 해제
        spriteRenderer.color = new Color(1, 1, 1, 1);
    }

    public void OnDie() // 캐릭터가 죽을때 사용하는 함수
    {
        // 투명
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        // 방향전환 Y축
        spriteRenderer.flipY = true;

        // 캡슐 콜라이더 비활성화
        capsuleCollider.enabled = false;

        // 죽을때 점프 효과
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);
    }

    public void VelocityZero() // 낙하속도를 0으로 설정하는 함수
    {
        rigid.velocity = Vector2.zero;
    }
}
