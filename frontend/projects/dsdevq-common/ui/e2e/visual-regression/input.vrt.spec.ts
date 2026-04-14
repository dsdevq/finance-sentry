import {expect, test} from '@playwright/test';

const STORIES = ['default', 'with-value', 'disabled', 'readonly', 'error', 'password', 'all-sizes'];

for (const story of STORIES) {
  test(`input/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-input--${story}&viewMode=story`);
    await page.waitForSelector('cmn-input, input', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`input-${story}-light.png`);
  });

  test(`input/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-input--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-input, input', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`input-${story}-dark.png`);
  });
}
