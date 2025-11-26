from PIL import Image
import os

# Load the guide image
guide_path = r"d:\Gits\BasicGameEngine_4_teamP\Assets\low_poly_guns_fbx\low_poly_guns_fbx\guide.jpg"
output_dir = r"d:\Gits\BasicGameEngine_4_teamP\Assets\FPS\Art\Textures\WeaponIcons"

# Create output directory if it doesn't exist
os.makedirs(output_dir, exist_ok=True)

# Load image
img = Image.open(guide_path)
width, height = img.size

print(f"Image size: {width}x{height}")

# The guide appears to be a grid of weapon icons
# We need to determine the grid layout by analyzing the image
# For now, let's assume it's a 4x4 or similar grid

# Calculate grid dimensions (you may need to adjust these based on the actual image)
cols = 4  # Number of columns
rows = 4  # Number of rows

cell_width = width // cols
cell_height = height // rows

print(f"Grid: {cols}x{rows}, Cell size: {cell_width}x{cell_height}")

# Weapon names (adjust based on actual weapons in the image)
weapon_names = [
    "Pistol", "Rifle", "Shotgun", "SMG",
    "Sniper", "Rocket", "Grenade", "Knife",
    "MachineGun", "AssaultRifle", "Revolver", "Crossbow",
    "Flamethrower", "LaserGun", "PlasmaRifle", "RailGun"
]

# Extract each weapon icon
index = 0
for row in range(rows):
    for col in range(cols):
        if index >= len(weapon_names):
            break
            
        # Calculate crop box
        left = col * cell_width
        top = row * cell_height
        right = left + cell_width
        bottom = top + cell_height
        
        # Crop the icon
        icon = img.crop((left, top, right, bottom))
        
        # Save the icon
        output_path = os.path.join(output_dir, f"{weapon_names[index]}_Icon.png")
        icon.save(output_path, "PNG")
        print(f"Saved: {weapon_names[index]}_Icon.png")
        
        index += 1

print(f"\nExtracted {index} weapon icons to {output_dir}")
