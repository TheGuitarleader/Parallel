# Contributing to Parallel

Thanks for your interest in contributing to Parallel! This project is built on transparency, modularity, and control. Whether you're fixing a bug, proposing a feature, or refining architecture, your input helps shape a tool that puts users in better control of their files.

## 🛠 Development Philosophy

1. Keep It Modular
- Break stuff into clean, reusable pieces.
- If it’s hard to test or swap out, it’s probably trying to do too much.
2. Make It Readable
- Code should be easy to follow—even after a long weekend.
- Comments explain why, not repeat what the code already says.
3. Refactor Without Fear
- If something feels clunky, fix it.
- Clean code isn’t a luxury—it’s how you stay sane.
4. Risk Logic Should Reflect Intent
- Don’t just crunch numbers—build systems that know when to lean in or back off.
- Confidence matters. Volatility matters. Make them talk to each other.
5. Protect the Flow
- Use PRs, reviews, and CI/CD to keep things smooth and predictable.
- Not about gatekeeping—just making sure changes are solid before they land.
6. Style Is Part of the System
- Whether it’s your gear, your UI, or your commit messages—make it feel like you.
- Expressive systems are easier to care about.


## 🧱 Getting Started

1. **Clone the repo**: 
    `git clone https://github.com/TheGuitarleader/Parallel.git`
2. Create a new branch:  
    `git checkout -b feature/your-feature-name`
    `git checkout -b patch/your-patch-name`
3. Make your changes with clear, modular commits.
4. Push and open a **Pull Request** (PR):
    `git push origin feature/your-feature-name`
    `git push origin patch/your-patch-name`

## 🔍 Pull Request Guidelines

- Use descriptive titles and commit messages.
- Reference related issues (e.g. `Fixed #42`).
- Include tests for new logic where applicable.
- Keep PRs focused—one feature or fix per PR.
- Be open to feedback and iteration.

## 🧪 Testing Expectations

Parallel uses modular test suites. Before submitting a PR:
- Run all relevant tests.
- Add new ones if your logic introduces new behavior.
- Validate edge cases and failure modes.

## 🧭 Branch Protection & CI/CD

All changes go through PR review. CI/CD pipelines validate builds across platforms. Please ensure your changes pass all checks before requesting review.

## 💬 Communication

Use [GitHub Issues](https://github.com/TheGuitarleader/Parallel/issues) for bugs, ideas, and discussion. We welcome thoughtful dialogue and collaborative troubleshooting.