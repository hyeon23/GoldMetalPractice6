using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("@[Core]")]
    public int score;
    public int maxLevel;
    public bool isOver;
    [Header("@[Object Pooling]")]
    public GameObject dongleAPrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;

    [Range(1,30)]//OnDisable �Լ����� ���� ����, Ʈ������, ���� �ʱ�ȭ
    public int poolSize;
    public int poolCursor;
    public Dongle lastDongle;

    [Header("@[Audio]")]
    //Sounds Variables
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum SFX { LevelUp, Next, Attach, Button, GameOver };
    int sfxCursor;

    [Header("@[UI]")]
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;
    public GameObject endGroup;
    public GameObject startGroup;

    [Header("@[ETC]")]
    public GameObject[] backGrounds;

    private void Awake()
    { 
        //������ Frame�� �ε巴�� �ϱ�
        //1. Application.targetFrameRage: ������ FPS�� �����ϴ� �Լ�
        //��� �÷��������� 60���� ����
        Application.targetFrameRate = 60;

        //Prefab�� interpolate �Ӽ��� none���� interpolate�� �ٲٸ� �������� �ε巴�� ��������

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        for(int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }
        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        //������Ʈ Ȱ��ȭ
        foreach(GameObject backGround in backGrounds)
            backGround.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        bgmPlayer.Play();//Audio Source ���
        SfxPlay(SFX.Button);
        Invoke("NextDongle", 1.5f);
    }


    void NextDongle()
    {
        if (isOver)
            return;

        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(GameManager.SFX.Next);
        StartCoroutine(WaitNext());
    }

    IEnumerator WaitNext()
    {
        while (lastDongle != null)//
        {
            yield return null;
        }
        yield return new WaitForSeconds(1.5f);

        NextDongle();
    }

    Dongle MakeDongle()
    {
        //����Ʈ ����
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);//Instantiate�� ����� GameObject
        instantEffectObj.name = "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        //���� ����
        GameObject instantDongleObj = Instantiate(dongleAPrefab, dongleGroup);//Instantiate�� ����� GameObject
        instantDongleObj.name = "Dongle " + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        instantDongle.manager = this;
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    //Random Dongle ����
    Dongle GetDongle()
    {
        for(int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count; ;
            if (!donglePool[poolCursor].gameObject.activeSelf)//�ش� ������Ʈ�� Ȱ��ȭ�Ǿ����� �˷��ִ� activeSelf
                return donglePool[poolCursor];
        }
        return MakeDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drop();
        lastDongle = null;
    }

    public void GameOver()
    {
        if (isOver)
            return;

        isOver = true;

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        //1. ��� �ȿ� �ִ� ��� ���� ��������<�߿�>
        Dongle[] dongles = FindObjectsOfType<Dongle>();//GameObject�� ���� ����
        //Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

        //2. ����� �� �ر� & �ռ� ������ ���� ��� ������ ����ȿ�� ��Ȱ��ȭ
        foreach (Dongle dongle in dongles)
        {
            dongle.rigid.simulated = false;
        }

        //3. ��� ���ۿ� �ϳ��� ������ �����ֱ�
        foreach (Dongle dongle in dongles)
        {
            dongle.Hide(Vector3.up * 100);//Tip: �ش� �Լ��� ���� ���ϴ� ������� ������ ���� ���
            yield return new WaitForSeconds(0.1f);
        }
        //�ƿ� ������ ���� ���� ����ְ�, �� �Լ��� if���� �߰��� �����ϴ� ��ĵ� ����

        //for(int index = 0; index < dongles.Length; index++)
        //{
        //    dongles[index].Hide(Vector3.up * 100);
        //}

        yield return new WaitForSeconds(1f);

        //4. �ְ� ���� ����
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        PlayerPrefs.SetInt("MaxScore", maxScore);
        
        //5. ���� ���� UI ǥ��
        subScoreText.text = "����: " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(GameManager.SFX.GameOver);
    }

    public void Reset()
    {
        SfxPlay(SFX.Button);
        StartCoroutine(ResetCoroutine());
    }

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(0);

    }

    public void SfxPlay(SFX type)
    {
        switch (type)
        {
            case SFX.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case SFX.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case SFX.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case SFX.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case SFX.GameOver:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }
        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;// sfxCursor = 0, 1, 2
    }

    private void LateUpdate()//Update ���� �� ����: ����, ��ġ�� Update���� ����ϸ�, �̸� ������ Ȱ���ϴ� ���� LateUpdate
    {
        scoreText.text = score.ToString();
    }

    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Application.Quit();//����� ȯ�濡�� ���� ������
        }
    }
}
