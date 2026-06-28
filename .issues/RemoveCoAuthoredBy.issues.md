# RemoveCoAuthoredBy 執行問題紀錄

## 失敗記錄 1

- **失敗的方法**：直接執行 `git filter-branch --msg-filter` 進行 commit message 改寫。
- **步驟**：步驟 2
- **原因**：工作區有未提交的修改（`doc/diagrams.md` 與 `tree.md`），導致 Git 拒絕執行改寫分支操作，錯誤訊息如下：
  `Cannot rewrite branches: You have unstaged changes.`
- **對策**：先執行 `git stash` 暫存工作區變更，執行完改寫後，再以 `git stash pop` 還原工作區。
