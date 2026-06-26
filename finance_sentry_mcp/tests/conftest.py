import pytest

from finance_sentry_mcp.server import app


@pytest.fixture
def all_registered_tools():
    return [v for k, v in app._local_provider._components.items() if k.startswith("tool:")]
