import {expect, test} from '@playwright/test';

const STORIES = ['default', 'with-hint', 'with-error', 'required', 'disabled', 'full-reactive-form-example'];

for (const story of STORIES) {
  test(`form-field/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-form-field--${story}&viewMode=story`);
    await page.waitForSelector('cmn-form-field, label', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`form-field-${story}-light.png`);
  });

  test(`form-field/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-form-field--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-form-field, label', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`form-field-${story}-dark.png`);
  });
}
