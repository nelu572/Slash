# Slash

## Git 관리 규칙

### 1. Git Commit 규칙

| 타입 | 사용 목적 | 예시 |
| --- | --- | --- |
| `feat` | 새로운 기능이 추가됨 | `feat: 점프 기능 구현` |
| `fix` | 버그 수정 | `fix: 체력 감소 오류 수정` |
| `refactor` | 기능 변경 없이 코드 개선 | `refactor: 전투 로직 정리` |
| `docs` | 문서 작성 및 수정 | `docs: README 파일 작성` |
| `chore` | 맵 수정 및 에셋과 관련된 내용 | `chore: 캐릭터 애니메이션 추가` |

### 2. Branch 규칙

이 프로젝트 에서는 기능이나 작업별로 Branch를 나누지 않고 `develop`에 직접 커밋 한다.

| Branch | 사용 목적 |
| --- | --- |
| `main` | 최종 완성된 버전 관리 |
| `develop` | 실제 개발 영역 |
